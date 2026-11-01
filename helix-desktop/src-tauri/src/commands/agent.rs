use std::ffi::c_void;
use std::fs::OpenOptions;
use std::io::Read;
use std::sync::Mutex;

use serde::{Deserialize, Serialize};
use tauri::{State, WebviewWindow};

const MAIN_WINDOW: &str = "main";
const AGENT_HOST: &str = "127.0.0.1";
const AGENT_PORT: u16 = 5080;
const PIPE_PATH: &str = r"\\.\pipe\helix-desktop-session";
const MAX_REQUEST_BYTES: usize = 4 * 1024;
const MAX_RESPONSE_BYTES: usize = 64 * 1024;
const MAX_SESSION_RESPONSE_BYTES: u64 = 2 * 1024;
const WINHTTP_ADDREQ_FLAG_ADD: u32 = 0x2000_0000;
const WINHTTP_ACCESS_TYPE_NO_PROXY: u32 = 1;
const WINHTTP_OPTION_DISABLE_FEATURE: u32 = 63;
const WINHTTP_DISABLE_REDIRECTS: u32 = 0x0000_0002;
const WINHTTP_QUERY_STATUS_CODE: u32 = 19;

fn set_request_redirect_policy<H>(
    request: H,
    set_option: impl FnOnce(H, u32, u32) -> bool,
) -> bool {
    set_option(request, WINHTTP_OPTION_DISABLE_FEATURE, WINHTTP_DISABLE_REDIRECTS)
}

struct NativeSession {
    token: String,
    verified_scientist: Option<String>,
    expires_at: Option<String>,
    restored: bool,
}

struct BrokerSessionState {
    session: Option<NativeSession>,
    restoration_error: Option<String>,
}

pub struct AgentBrokerState(Mutex<BrokerSessionState>);

impl AgentBrokerState {
    pub fn new() -> Self {
        Self(Mutex::new(BrokerSessionState {
            session: None,
            restoration_error: None,
        }))
    }
}

#[derive(Deserialize)]
struct PipeSessionResponse {
    token: String,
    #[serde(default, alias = "verifiedScientist", alias = "verifiedActorDisplayName")]
    verified_scientist: Option<String>,
    #[serde(default, alias = "expiresAt")]
    expires_at: Option<String>,
}

#[derive(Serialize)]
struct AgentSessionStatus {
    authenticated: bool,
    verified_scientist: Option<String>,
    expires_at: Option<String>,
    restored: bool,
    restoration_error: Option<String>,
}

fn require_main_window(window: &WebviewWindow) -> Result<(), String> {
    if window.label() == MAIN_WINDOW { Ok(()) }
    else { Err("Agent broker commands are restricted to the main local window.".to_string()) }
}

fn require_session(state: &State<'_, AgentBrokerState>) -> Result<String, String> {
    state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())?
        .session.as_ref().map(|session| session.token.clone())
        .ok_or_else(|| "No desktop Agent session is available.".to_string())
}

fn validate_json(value: &str) -> Result<(), String> {
    if value.len() > MAX_REQUEST_BYTES { return Err("Agent request exceeds the permitted size.".to_string()); }
    serde_json::from_str::<serde_json::Value>(value)
        .map(|_| ()).map_err(|_| "Agent request must be valid JSON.".to_string())
}

fn validate_command_id(command_id: &str) -> Result<(), String> {
    let valid_hyphens = [8, 13, 18, 23];
    if command_id.len() != 36 || !command_id.bytes().enumerate().all(|(index, byte)| {
        if valid_hyphens.contains(&index) { byte == b'-' } else { byte.is_ascii_hexdigit() }
    }) {
        return Err("Command identifier is invalid.".to_string());
    }
    Ok(())
}

fn validate_cursor(cursor: &str) -> Result<(), String> {
    if cursor.len() > 256 || !cursor.bytes().all(|byte| byte.is_ascii_alphanumeric() || matches!(byte, b'-' | b'_' | b':')) {
        return Err("Evidence cursor is invalid.".to_string());
    }
    Ok(())
}

fn validate_device_id(device_id: &str) -> Result<(), String> {
    if device_id.is_empty() || device_id.len() > 128 || !device_id.bytes().all(|byte| byte.is_ascii_alphanumeric() || matches!(byte, b'-' | b'_')) {
        return Err("Device identifier is invalid.".to_string());
    }
    Ok(())
}

fn require_success_status(status: u16) -> Result<(), String> {
    if (200..300).contains(&status) {
        Ok(())
    } else {
        Err(format!("Local Agent rejected the request (HTTP {status})."))
    }
}

fn validate_page_size(page_size: u16) -> Result<(), String> {
    if !(1..=100).contains(&page_size) {
        return Err("Sample page size is invalid.".to_string());
    }
    Ok(())
}

fn validate_sample_mutation_request(request: &str, requires_sequence: bool) -> Result<(), String> {
    validate_json(request)?;
    let value: serde_json::Value = serde_json::from_str(request)
        .map_err(|_| "Sample request is invalid.".to_string())?;
    let object = value.as_object().ok_or_else(|| "Sample request is invalid.".to_string())?;
    let operation_id = object.get("operationId").and_then(serde_json::Value::as_str)
        .ok_or_else(|| "Sample operation identifier is required.".to_string())?;
    validate_command_id(operation_id)?;
    let location = object.get("location").and_then(serde_json::Value::as_str)
        .ok_or_else(|| "Sample location is required.".to_string())?;
    if location.trim().is_empty() || location.len() > 512 {
        return Err("Sample location is invalid.".to_string());
    }
    let occurred_at = object.get("occurredAtUtc").and_then(serde_json::Value::as_str)
        .ok_or_else(|| "Sample occurrence time is required.".to_string())?;
    if occurred_at.is_empty() || occurred_at.len() > 64 {
        return Err("Sample occurrence time is invalid.".to_string());
    }
    if requires_sequence {
        let kind = object.get("kind").and_then(serde_json::Value::as_i64)
            .or_else(|| object.get("kind").and_then(serde_json::Value::as_str).and_then(|value| match value {
                "Collected" => Some(0), "Moved" => Some(1), "Transferred" => Some(2), "Consumed" => Some(3), "Corrected" => Some(4), _ => None,
            }))
            .ok_or_else(|| "Sample event kind is required.".to_string())?;
        if !(0..=4).contains(&kind) {
            return Err("Sample event kind is invalid.".to_string());
        }
        if object.get("expectedSequence").and_then(serde_json::Value::as_u64).is_none() {
            return Err("Sample event sequence is required.".to_string());
        }
    }
    Ok(())
}

fn validate_sample_transformation_request(request: &str) -> Result<(), String> {
    validate_json(request)?;
    let value: serde_json::Value = serde_json::from_str(request)
        .map_err(|_| "Sample transformation request is invalid.".to_string())?;
    let object = value.as_object().ok_or_else(|| "Sample transformation request is invalid.".to_string())?;
    for field in ["operationId", "transformationId"] {
        let id = object.get(field).and_then(serde_json::Value::as_str)
            .ok_or_else(|| format!("Sample transformation {field} is required."))?;
        validate_command_id(id)?;
    }
    let operation = object.get("operation").and_then(serde_json::Value::as_str)
        .ok_or_else(|| "Sample transformation operation is required.".to_string())?;
    if operation.trim().is_empty() || operation.len() > 512 {
        return Err("Sample transformation operation is invalid.".to_string());
    }
    for field in ["parentSampleIds", "childSampleIds"] {
        let ids = object.get(field).and_then(serde_json::Value::as_array)
            .ok_or_else(|| format!("Sample transformation {field} is required."))?;
        if ids.is_empty() || ids.len() > 32 { return Err(format!("Sample transformation {field} is invalid.")); }
        for id in ids {
            validate_command_id(id.as_str().ok_or_else(|| format!("Sample transformation {field} is invalid."))?)?;
        }
    }
    Ok(())
}

fn validate_lineage_direction(direction: &str) -> Result<(), String> {
    if matches!(direction, "Ancestors" | "Descendants") { Ok(()) }
    else { Err("Sample lineage direction is invalid.".to_string()) }
}

fn scientific_kind_path(kind: &str) -> Result<&'static str, String> {
    match kind {
        "procedure" | "procedures" => Ok("procedures"),
        "plan" | "plans" => Ok("plans"),
        "policy" | "policies" => Ok("policies"),
        "assembly" | "assemblies" => Ok("assemblies"),
        _ => Err("Scientific kind is invalid.".to_string()),
    }
}

fn mutable_scientific_kind_path(kind: &str) -> Result<&'static str, String> {
    match kind {
        "procedure" | "procedures" | "plan" | "plans" => scientific_kind_path(kind),
        _ => Err("Scientific kind cannot be mutated through this command.".to_string()),
    }
}

fn validate_assembly_request(request: &str) -> Result<(), String> {
    if request.len() > 64 * 1024 { return Err("Assembly request exceeds the permitted size.".to_string()); }
    let value: serde_json::Value = serde_json::from_str(request)
        .map_err(|_| "Assembly request must be valid JSON.".to_string())?;
    let object = value.as_object().ok_or_else(|| "Assembly request is invalid.".to_string())?;
    for field in ["operationId", "apparatusRevisionId", "procedureRevisionId", "planRevisionId", "policyRevisionId"] {
        let id = object.get(field).and_then(serde_json::Value::as_str)
            .ok_or_else(|| format!("Assembly {field} is required."))?;
        validate_command_id(id)?;
    }
    Ok(())
}

#[derive(Deserialize)]
struct DeriveProcedureRequest {
    #[serde(alias = "operationId")]
    operation_id: String,
    #[serde(alias = "sourceRevisionId")]
    source_revision_id: String,
    name: String,
}

#[derive(Deserialize)]
struct AdoptProcedureRequest {
    #[serde(alias = "operationId")]
    operation_id: String,
    #[serde(alias = "procedureRevisionId")]
    procedure_revision_id: String,
    #[serde(alias = "expectedToken")]
    expected_token: i64,
}

fn validate_derive_procedure_request(request: &str) -> Result<(), String> {
    validate_json(request)?;
    let value: DeriveProcedureRequest = serde_json::from_str(request).map_err(|_| "Procedure derivation request is invalid.".to_string())?;
    validate_command_id(&value.operation_id)?;
    validate_command_id(&value.source_revision_id)?;
    if value.name.trim().is_empty() || value.name.len() > 256 { return Err("Derived procedure name is invalid.".to_string()); }
    Ok(())
}

fn validate_adopt_procedure_request(request: &str) -> Result<(), String> {
    validate_json(request)?;
    let value: AdoptProcedureRequest = serde_json::from_str(request).map_err(|_| "Procedure adoption request is invalid.".to_string())?;
    validate_command_id(&value.operation_id)?;
    validate_command_id(&value.procedure_revision_id)?;
    if value.expected_token < 0 { return Err("Expected plan token is invalid.".to_string()); }
    Ok(())
}

fn session_status(state: &State<'_, AgentBrokerState>) -> Result<String, String> {
    let state = state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())?;
    let status = AgentSessionStatus {
        authenticated: state.session.is_some(),
        verified_scientist: state.session.as_ref().and_then(|session| session.verified_scientist.clone()),
        expires_at: state.session.as_ref().and_then(|session| session.expires_at.clone()),
        restored: state.session.as_ref().is_some_and(|session| session.restored),
        restoration_error: state.restoration_error.clone(),
    };
    serde_json::to_string(&status).map_err(|_| "Unable to report local session status.".to_string())
}

#[tauri::command]
pub async fn bootstrap_agent_session(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<(), String> {
    require_main_window(&window)?;
    let pipe = match OpenOptions::new().read(true).open(PIPE_PATH) {
        Ok(pipe) => pipe,
        Err(_) => {
            if let Ok(mut broker) = state.0.lock() {
                broker.restoration_error = Some("Unable to establish the local desktop session.".to_string());
            }
            return Err("Unable to establish the local desktop session.".to_string());
        }
    };
    let mut response = String::new();
    if pipe.take(MAX_SESSION_RESPONSE_BYTES).read_to_string(&mut response).is_err() {
        if let Ok(mut broker) = state.0.lock() {
            broker.restoration_error = Some("Unable to read the local desktop session.".to_string());
        }
        return Err("Unable to read the local desktop session.".to_string());
    }
    let parsed = serde_json::from_str::<PipeSessionResponse>(&response).ok();
    let token = parsed.as_ref().map(|session| session.token.as_str())
        .or_else(|| response.lines().next())
        .filter(|value| !value.is_empty() && value.len() <= 1024 && !value.chars().any(char::is_control))
        .ok_or_else(|| "Local desktop session response was invalid.".to_string())?;
    let session = NativeSession {
        token: token.to_string(),
        verified_scientist: parsed.and_then(|value| value.verified_scientist),
        expires_at: serde_json::from_str::<PipeSessionResponse>(&response).ok().and_then(|value| value.expires_at),
        restored: true,
    };
    let mut broker = state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())?;
    broker.session = Some(session);
    broker.restoration_error = None;
    Ok(())
}

#[tauri::command]
pub async fn agent_session_status(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<String, String> {
    require_main_window(&window)?;
    session_status(&state)
}

#[tauri::command]
pub async fn agent_logout(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<(), String> {
    require_main_window(&window)?;
    let token = require_session(&state)?;
    native_request("POST", "/session/logout", None, &token)?;
    let mut broker = state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())?;
    broker.session = None;
    Ok(())
}

#[tauri::command]
pub async fn agent_revoke_session(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<(), String> {
    require_main_window(&window)?;
    let token = require_session(&state)?;
    native_request("POST", "/session/revoke", None, &token)?;
    let mut broker = state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())?;
    broker.session = None;
    Ok(())
}

#[tauri::command]
pub async fn agent_status(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<String, String> {
    require_main_window(&window)?;
    native_request("GET", "/health", None, &require_session(&state)?)
}

/// Submits bounded apparatus-only scientific content through the fixed authenticated Agent route.
#[tauri::command]
pub async fn scientific_create(window: WebviewWindow, state: State<'_, AgentBrokerState>, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    if request.len() > 64 * 1024 { return Err("Scientific request exceeds the permitted size.".to_string()); }
    validate_json(&request)?;
    native_request("POST", "/api/scientific-setup/apparatus/", Some(&request), &require_session(&state)?)
}

/// Reads immutable scientific draft content through a fixed, authenticated local route.
#[tauri::command]
pub async fn scientific_read_draft(window: WebviewWindow, state: State<'_, AgentBrokerState>, apparatus_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&apparatus_id)?;
    native_request("GET", &format!("/api/scientific-setup/apparatus/{apparatus_id}/draft"), None, &require_session(&state)?)
}

/// Saves a scientist proposal; validation and commit authority remain in the local Agent.
#[tauri::command]
pub async fn scientific_save_draft(window: WebviewWindow, state: State<'_, AgentBrokerState>, apparatus_id: String, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&apparatus_id)?;
    if request.len() > 64 * 1024 { return Err("Scientific request exceeds the permitted size.".to_string()); }
    validate_json(&request)?;
    native_request("PUT", &format!("/api/scientific-setup/apparatus/{apparatus_id}/draft"), Some(&request), &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_publish(window: WebviewWindow, state: State<'_, AgentBrokerState>, apparatus_id: String, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&apparatus_id)?;
    if request.len() > 64 * 1024 { return Err("Scientific request exceeds the permitted size.".to_string()); }
    validate_json(&request)?;
    native_request("POST", &format!("/api/scientific-setup/apparatus/{apparatus_id}/publications"), Some(&request), &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_list(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<String, String> {
    require_main_window(&window)?;
    native_request("GET", "/api/scientific-setup/apparatus", None, &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_read(window: WebviewWindow, state: State<'_, AgentBrokerState>, apparatus_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&apparatus_id)?;
    native_request("GET", &format!("/api/scientific-setup/apparatus/{apparatus_id}"), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_history(window: WebviewWindow, state: State<'_, AgentBrokerState>, apparatus_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&apparatus_id)?;
    native_request("GET", &format!("/api/scientific-setup/apparatus/{apparatus_id}/publications"), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_read_revision(window: WebviewWindow, state: State<'_, AgentBrokerState>, apparatus_id: String, revision: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&apparatus_id)?;
    validate_command_id(&revision)?;
    native_request("GET", &format!("/api/scientific-setup/apparatus/{apparatus_id}/publications/{revision}"), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_operation(window: WebviewWindow, state: State<'_, AgentBrokerState>, operation_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&operation_id)?;
    native_request("GET", &format!("/api/scientific-setup/operations/{operation_id}"), None, &require_session(&state)?)
}

/// Fixed-kind lifecycle commands never accept a renderer-supplied URL or HTTP verb.
#[tauri::command]
pub async fn scientific_list_kind(window: WebviewWindow, state: State<'_, AgentBrokerState>, kind: String) -> Result<String, String> {
    require_main_window(&window)?;
    native_request("GET", &format!("/api/scientific-setup/{}", scientific_kind_path(&kind)?), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_create_kind(window: WebviewWindow, state: State<'_, AgentBrokerState>, kind: String, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    if request.len() > 64 * 1024 { return Err("Scientific request exceeds the permitted size.".to_string()); }
    validate_json(&request)?;
    native_request("POST", &format!("/api/scientific-setup/{}", mutable_scientific_kind_path(&kind)?), Some(&request), &require_session(&state)?)
}

/// Creates a source-linked procedure draft through the only allowed derive route.
#[tauri::command]
pub async fn scientific_derive_procedure(window: WebviewWindow, state: State<'_, AgentBrokerState>, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_derive_procedure_request(&request)?;
    native_request("POST", "/api/scientific-setup/procedures/derive", Some(&request), &require_session(&state)?)
}

/// Re-pins one plan draft after its explicit compatibility check; it never publishes the plan.
#[tauri::command]
pub async fn scientific_adopt_procedure(window: WebviewWindow, state: State<'_, AgentBrokerState>, plan_id: String, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&plan_id)?;
    validate_adopt_procedure_request(&request)?;
    native_request("POST", &format!("/api/scientific-setup/plans/{plan_id}/adopt-procedure"), Some(&request), &require_session(&state)?)
}

/// Reads a consistent, Agent-authoritative assembly preview. This command never issues approval.
#[tauri::command]
pub async fn scientific_assembly_defaults(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<String, String> {
    require_main_window(&window)?;
    native_request("GET", "/api/scientific-setup/assembly-defaults", None, &require_session(&state)?)
}

/// Submits only an exact immutable-reference assembly proposal to the fixed local Agent endpoint.
#[tauri::command]
pub async fn scientific_assemble(window: WebviewWindow, state: State<'_, AgentBrokerState>, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_assembly_request(&request)?;
    native_request("POST", "/api/scientific-setup/assemblies", Some(&request), &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_read_assembly(window: WebviewWindow, state: State<'_, AgentBrokerState>, assembly_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&assembly_id)?;
    native_request("GET", &format!("/api/scientific-setup/assemblies/{assembly_id}"), None, &require_session(&state)?)
}

/// Lists immutable policy content only; approval remains an Agent authority concern.
#[tauri::command]
pub async fn scientific_policies(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<String, String> {
    require_main_window(&window)?;
    native_request("GET", "/api/scientific-setup/policies", None, &require_session(&state)?)
}

#[tauri::command]
pub async fn scientific_read_policy(window: WebviewWindow, state: State<'_, AgentBrokerState>, revision_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&revision_id)?;
    native_request("GET", &format!("/api/scientific-setup/policies/{revision_id}"), None, &require_session(&state)?)
}

/// Lists only the authenticated scientist's sample projections through a fixed local route.
#[tauri::command]
pub async fn sample_list(window: WebviewWindow, state: State<'_, AgentBrokerState>, page_size: u16) -> Result<String, String> {
    require_main_window(&window)?;
    validate_page_size(page_size)?;
    native_request("GET", &format!("/api/samples/?pageSize={page_size}"), None, &require_session(&state)?)
}

/// Reads a single actor-scoped sample projection; IDs never become paths or arbitrary URLs.
#[tauri::command]
pub async fn sample_read(window: WebviewWindow, state: State<'_, AgentBrokerState>, sample_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&sample_id)?;
    native_request("GET", &format!("/api/samples/{sample_id}"), None, &require_session(&state)?)
}

/// Submits explicit collection evidence only; the Agent admits facts and preserves replay receipts.
#[tauri::command]
pub async fn sample_confirm(window: WebviewWindow, state: State<'_, AgentBrokerState>, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_sample_mutation_request(&request, false)?;
    native_request("POST", "/api/samples/confirmations", Some(&request), &require_session(&state)?)
}

/// Appends one bounded custody event to an exact sample identity through the fixed Agent route.
#[tauri::command]
pub async fn sample_append_event(window: WebviewWindow, state: State<'_, AgentBrokerState>, sample_id: String, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&sample_id)?;
    validate_sample_mutation_request(&request, true)?;
    native_request("POST", &format!("/api/samples/{sample_id}/events"), Some(&request), &require_session(&state)?)
}

/// Reads bounded custody history without accepting a renderer-supplied destination or cursor syntax.
#[tauri::command]
pub async fn sample_events(window: WebviewWindow, state: State<'_, AgentBrokerState>, sample_id: String, after_sequence: Option<u64>, page_size: u16) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&sample_id)?;
    validate_page_size(page_size)?;
    let after_sequence = after_sequence.map(|sequence| format!("&afterSequence={sequence}")).unwrap_or_default();
    native_request("GET", &format!("/api/samples/{sample_id}/events?pageSize={page_size}{after_sequence}"), None, &require_session(&state)?)
}

/// Reads an actor-scoped sample receipt through the fixed operation route; it never replays a mutation.
#[tauri::command]
pub async fn sample_operation(window: WebviewWindow, state: State<'_, AgentBrokerState>, operation_id: String, kind: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&operation_id)?;
    if kind != "sample.confirm" && kind != "sample.append" && kind != "sample.transform" {
        return Err("Sample operation kind is invalid.".to_string());
    }
    native_request("GET", &format!("/api/scientific-operations/{operation_id}?kind={kind}"), None, &require_session(&state)?)
}

/// Proposes a bounded, new-child-only transformation through the fixed Agent route.
#[tauri::command]
pub async fn sample_transform(window: WebviewWindow, state: State<'_, AgentBrokerState>, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_sample_transformation_request(&request)?;
    native_request("POST", "/api/samples/transformations", Some(&request), &require_session(&state)?)
}

/// Reads bounded ancestor or descendant evidence without accepting a renderer-provided route.
#[tauri::command]
pub async fn sample_lineage(window: WebviewWindow, state: State<'_, AgentBrokerState>, sample_id: String, direction: String, continuation: Option<String>, page_size: u16) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&sample_id)?;
    validate_lineage_direction(&direction)?;
    validate_page_size(page_size)?;
    let continuation = match continuation {
        Some(value) => { validate_command_id(&value)?; format!("&continuation={value}") },
        None => String::new(),
    };
    native_request("GET", &format!("/api/samples/{sample_id}/lineage?direction={direction}&pageSize={page_size}{continuation}"), None, &require_session(&state)?)
}

/// Lists only the Agent's canonical simulator adapters through the authenticated broker.
#[tauri::command]
pub async fn agent_devices(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<String, String> {
    require_main_window(&window)?;
    native_request("GET", "/devices", None, &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_device_capabilities(window: WebviewWindow, state: State<'_, AgentBrokerState>, device_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_device_id(&device_id)?;
    native_request("GET", &format!("/devices/{device_id}/capabilities"), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_propose(window: WebviewWindow, state: State<'_, AgentBrokerState>, proposal: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_json(&proposal)?;
    native_request("POST", "/commands/propose", Some(&proposal), &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_approve_policy(window: WebviewWindow, state: State<'_, AgentBrokerState>, command_id: String, approval: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&command_id)?;
    validate_json(&approval)?;
    native_request("POST", &format!("/commands/{command_id}/approve-policy"), Some(&approval), &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_dispatch(window: WebviewWindow, state: State<'_, AgentBrokerState>, command_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&command_id)?;
    native_request("POST", &format!("/commands/{command_id}/dispatch"), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_evidence(window: WebviewWindow, state: State<'_, AgentBrokerState>, command_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&command_id)?;
    native_request("GET", &format!("/commands/{command_id}/evidence"), None, &require_session(&state)?)
}

/// A status query is intentionally a read of recorded evidence, not a retry of dispatch.
#[tauri::command]
pub async fn agent_operation_status(window: WebviewWindow, state: State<'_, AgentBrokerState>, operation_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&operation_id)?;
    native_request("GET", &format!("/commands/{operation_id}/evidence"), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_evidence_cursor(window: WebviewWindow, state: State<'_, AgentBrokerState>, cursor: Option<String>, page_size: u16, execution_id: Option<String>) -> Result<String, String> {
    require_main_window(&window)?;
    if !(1..=100).contains(&page_size) {
        return Err("Evidence page size is invalid.".to_string());
    }
    let cursor = cursor.unwrap_or_default();
    validate_cursor(&cursor)?;
    let execution_query = match execution_id {
        Some(id) => { validate_command_id(&id)?; format!("&executionId={id}") },
        None => String::new(),
    };
    let path = format!("/commands/evidence?cursor={cursor}&pageSize={page_size}{execution_query}");
    native_request("GET", &path, None, &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_append_review_note(window: WebviewWindow, state: State<'_, AgentBrokerState>, command_id: String, note: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&command_id)?;
    validate_json(&note)?;
    native_request("POST", &format!("/commands/{command_id}/review-notes"), Some(&note), &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_experiment_status(window: WebviewWindow, state: State<'_, AgentBrokerState>, experiment_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&experiment_id)?;
    native_request("GET", &format!("/experiments/{experiment_id}"), None, &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_experiment_start(window: WebviewWindow, state: State<'_, AgentBrokerState>, request: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_json(&request)?;
    native_request("POST", "/experiments", Some(&request), &require_session(&state)?)
}

#[tauri::command]
pub async fn agent_experiment_control(window: WebviewWindow, state: State<'_, AgentBrokerState>, experiment_id: String, control: String, operation_id: String) -> Result<String, String> {
    require_main_window(&window)?;
    validate_command_id(&experiment_id)?;
    validate_command_id(&operation_id)?;
    if !matches!(control.as_str(), "pause" | "hold" | "resume" | "abort") {
        return Err("Experiment control is invalid.".to_string());
    }
    // The Agent owns the durable execution revision. The renderer-provided operation id is
    // validated above for correlation but is deliberately not interpreted as authority.
    native_request("POST", &format!("/executions/{experiment_id}/control/{control}"), None, &require_session(&state)?)
}

#[cfg(windows)]
fn wide(value: &str) -> Vec<u16> { value.encode_utf16().chain(std::iter::once(0)).collect() }

#[cfg(windows)]
fn native_request(verb: &str, path: &str, body: Option<&str>, token: &str) -> Result<String, String> {
    type Handle = *mut c_void;
    #[link(name = "winhttp")]
    unsafe extern "system" {
        fn WinHttpOpen(agent: *const u16, access: u32, proxy: *const u16, bypass: *const u16, flags: u32) -> Handle;
        fn WinHttpConnect(session: Handle, server: *const u16, port: u16, reserved: u32) -> Handle;
        fn WinHttpSetOption(handle: Handle, option: u32, buffer: *mut c_void, buffer_length: u32) -> i32;
        fn WinHttpSetTimeouts(handle: Handle, resolve_timeout: i32, connect_timeout: i32, send_timeout: i32, receive_timeout: i32) -> i32;
        fn WinHttpOpenRequest(connect: Handle, verb: *const u16, object: *const u16, version: *const u16, referer: *const u16, accept: *const *const u16, flags: u32) -> Handle;
        fn WinHttpAddRequestHeaders(request: Handle, headers: *const u16, length: u32, modifiers: u32) -> i32;
        fn WinHttpSendRequest(request: Handle, headers: *const u16, length: u32, optional: *mut c_void, optional_length: u32, total_length: u32, context: usize) -> i32;
        fn WinHttpReceiveResponse(request: Handle, reserved: *mut c_void) -> i32;
        fn WinHttpQueryHeaders(request: Handle, info_level: u32, name: *const u16, buffer: *mut c_void, buffer_length: *mut u32, index: *mut u32) -> i32;
        fn WinHttpQueryDataAvailable(request: Handle, available: *mut u32) -> i32;
        fn WinHttpReadData(request: Handle, buffer: *mut c_void, bytes_to_read: u32, bytes_read: *mut u32) -> i32;
        fn WinHttpCloseHandle(handle: Handle) -> i32;
    }

    let user_agent = wide("HelixDesktop/1.0");
    let host = wide(AGENT_HOST);
    let verb = wide(verb);
    let path = wide(path);
    let headers = wide(&format!("Authorization: Bearer {token}\r\nContent-Type: application/json\r\n"));
    let payload = body.unwrap_or("").as_bytes();
    unsafe {
        let session = WinHttpOpen(user_agent.as_ptr(), WINHTTP_ACCESS_TYPE_NO_PROXY, std::ptr::null(), std::ptr::null(), 0);
        if session.is_null() {
            let error = std::io::Error::last_os_error();
            return Err(format!("Unable to open fixed local Agent transport (OS error: {error})."));
        }
        if WinHttpSetTimeouts(session, 5_000, 5_000, 5_000, 5_000) == 0 {
            let error = std::io::Error::last_os_error();
            WinHttpCloseHandle(session);
            return Err(format!("Unable to set timeouts for fixed local Agent transport (OS error: {error})."));
        }
        let connect = WinHttpConnect(session, host.as_ptr(), AGENT_PORT, 0);
        if connect.is_null() {
            let error = std::io::Error::last_os_error();
            WinHttpCloseHandle(session);
            return Err(format!("Unable to connect to the fixed local Agent (OS error: {error})."));
        }
        let request = WinHttpOpenRequest(connect, verb.as_ptr(), path.as_ptr(), std::ptr::null(), std::ptr::null(), std::ptr::null(), 0);
        if request.is_null() {
            let error = std::io::Error::last_os_error();
            WinHttpCloseHandle(connect);
            WinHttpCloseHandle(session);
            return Err(format!("Unable to create local Agent request (OS error: {error})."));
        }
        // Redirect suppression is request-scoped in WinHTTP; fail closed before sending.
        let redirects_disabled = set_request_redirect_policy(request, |handle, option, value| {
            let mut value = value;
            WinHttpSetOption(
                handle,
                option,
                &mut value as *mut u32 as *mut c_void,
                std::mem::size_of::<u32>() as u32,
            ) != 0
        });
        if !redirects_disabled {
            let error = std::io::Error::last_os_error();
            WinHttpCloseHandle(request);
            WinHttpCloseHandle(connect);
            WinHttpCloseHandle(session);
            return Err(format!("Unable to disable redirects for the fixed local Agent request (OS error: {error})."));
        }
        // WinHttpAddRequestHeaders requires an explicit add/replace modifier.
        // Passing zero rejects the request headers on Windows, which meant the
        // authenticated proposal was never sent to the Agent.
        let sent = WinHttpAddRequestHeaders(request, headers.as_ptr(), u32::MAX, WINHTTP_ADDREQ_FLAG_ADD) != 0 &&
            WinHttpSendRequest(request, std::ptr::null(), 0, payload.as_ptr() as *mut c_void, payload.len() as u32, payload.len() as u32, 0) != 0;
        let received = sent && WinHttpReceiveResponse(request, std::ptr::null_mut()) != 0;
        if !received {
            // Retain the OS error only as a local diagnostic; it identifies a
            // transport failure without disclosing the opaque session token.
            let error = std::io::Error::last_os_error();
            WinHttpCloseHandle(request); WinHttpCloseHandle(connect); WinHttpCloseHandle(session);
            return Err(format!("Local Agent request failed ({error})."));
        }
        let mut status_buffer = [0u16; 4];
        let mut status_length = (status_buffer.len() * std::mem::size_of::<u16>()) as u32;
        if WinHttpQueryHeaders(request, WINHTTP_QUERY_STATUS_CODE, std::ptr::null(), status_buffer.as_mut_ptr() as *mut c_void, &mut status_length, std::ptr::null_mut()) == 0 {
            WinHttpCloseHandle(request); WinHttpCloseHandle(connect); WinHttpCloseHandle(session);
            return Err("Unable to read local Agent response status.".to_string());
        }
        let status = String::from_utf16_lossy(&status_buffer[..(status_length as usize / std::mem::size_of::<u16>())])
            .trim_end_matches('\0').parse::<u16>().map_err(|_| "Local Agent returned an invalid response status.".to_string())?;
        if let Err(error) = require_success_status(status) {
            WinHttpCloseHandle(request); WinHttpCloseHandle(connect); WinHttpCloseHandle(session);
            return Err(error);
        }
        let mut response = Vec::new();
        loop {
            let mut available = 0u32;
            if WinHttpQueryDataAvailable(request, &mut available) == 0 { break; }
            if available == 0 { break; }
            if response.len().saturating_add(available as usize) > MAX_RESPONSE_BYTES {
                WinHttpCloseHandle(request); WinHttpCloseHandle(connect); WinHttpCloseHandle(session);
                return Err("Local Agent response exceeds the permitted size.".to_string());
            }
            let mut chunk = vec![0u8; available as usize];
            let mut read = 0u32;
            if WinHttpReadData(request, chunk.as_mut_ptr() as *mut c_void, available, &mut read) == 0 { break; }
            response.extend_from_slice(&chunk[..read as usize]);
        }
        WinHttpCloseHandle(request); WinHttpCloseHandle(connect); WinHttpCloseHandle(session);
        String::from_utf8(response).map_err(|_| "Local Agent returned an invalid response.".to_string())
    }
}

#[cfg(not(windows))]
fn native_request(_verb: &str, _path: &str, _body: Option<&str>, _token: &str) -> Result<String, String> {
    Err("The native Agent broker is supported only on Windows.".to_string())
}

#[cfg(test)]
mod agent_security {
    use super::*;
    #[test] fn fixed_agent_target_is_loopback_only_and_uses_no_proxy() {
        assert_eq!(AGENT_HOST, "127.0.0.1");
        assert_eq!(AGENT_PORT, 5080);
        assert_eq!(WINHTTP_ACCESS_TYPE_NO_PROXY, 1);
    }
    #[test] fn redirect_suppression_uses_request_handle_and_fails_closed() {
        let session_handle = 1usize;
        let request_handle = 2usize;
        let mut applied = None;

        let succeeded = set_request_redirect_policy(request_handle, |handle, option, feature| {
            applied = Some((handle, option, feature));
            true
        });

        assert!(succeeded);
        assert_eq!(applied, Some((request_handle, WINHTTP_OPTION_DISABLE_FEATURE, WINHTTP_DISABLE_REDIRECTS)));
        assert_ne!(applied.unwrap().0, session_handle);
        assert!(!set_request_redirect_policy(request_handle, |_, _, _| false));
    }
    #[test] fn broker_never_serializes_its_session_state() { assert!(AgentBrokerState::new().0.lock().unwrap().session.is_none()); }
    #[test] fn command_ids_do_not_accept_paths() { assert!(validate_command_id("../commands").is_err()); }
    #[test] fn command_ids_require_uuid_shape() { assert!(validate_command_id("------------------------------------").is_err()); }
    #[test] fn cursor_does_not_accept_query_injection() { assert!(validate_cursor("cursor&host=remote").is_err()); }
    #[test] fn device_ids_do_not_accept_paths() { assert!(validate_device_id("../real-device").is_err()); }
    #[test] fn scientific_kinds_are_fixed_and_reject_paths() {
        assert_eq!(scientific_kind_path("procedure"), Ok("procedures"));
        assert_eq!(scientific_kind_path("plan"), Ok("plans"));
        assert!(scientific_kind_path("../commands").is_err());
    }

    #[test] fn scientific_policy_and_assembly_routes_are_fixed_and_reject_paths() {
        assert_eq!(scientific_kind_path("policy"), Ok("policies"));
        assert_eq!(scientific_kind_path("assembly"), Ok("assemblies"));
        assert!(scientific_kind_path("policy/../commands").is_err());
    }

    #[test] fn sample_commands_require_typed_ids_and_bounded_requests() {
        let confirmation = r#"{"operationId":"11111111-1111-1111-1111-111111111111","location":"hood-a","occurredAtUtc":"2026-11-01T00:00:00Z"}"#;
        let append = r#"{"operationId":"11111111-1111-1111-1111-111111111111","kind":1,"location":"freezer-a","occurredAtUtc":"2026-11-01T00:00:00Z","expectedSequence":0}"#;
        assert!(validate_sample_mutation_request(confirmation, false).is_ok());
        assert!(validate_sample_mutation_request(append, true).is_ok());
        assert!(validate_sample_mutation_request(r#"{"operationId":"../foreign","location":"hood-a","occurredAtUtc":"2026-11-01T00:00:00Z"}"#, false).is_err());
        assert!(validate_sample_mutation_request(r#"{"operationId":"11111111-1111-1111-1111-111111111111","kind":9,"location":"hood-a","occurredAtUtc":"2026-11-01T00:00:00Z","expectedSequence":0}"#, true).is_err());
        assert!(validate_page_size(1).is_ok());
        assert!(validate_page_size(100).is_ok());
        assert!(validate_page_size(0).is_err());
        assert!(validate_page_size(101).is_err());
    }

    #[test] fn transformation_and_lineage_commands_use_fixed_bounded_inputs() {
        let transformation = r#"{"operationId":"11111111-1111-1111-1111-111111111111","transformationId":"22222222-2222-2222-2222-222222222222","operation":"split","parentSampleIds":["33333333-3333-3333-3333-333333333333"],"childSampleIds":["44444444-4444-4444-4444-444444444444"]}"#;
        assert!(validate_sample_transformation_request(transformation).is_ok());
        assert!(validate_sample_transformation_request(r#"{"operationId":"../foreign","transformationId":"22222222-2222-2222-2222-222222222222","operation":"split","parentSampleIds":["33333333-3333-3333-3333-333333333333"],"childSampleIds":["44444444-4444-4444-4444-444444444444"]}"#).is_err());
        assert!(validate_lineage_direction("Ancestors").is_ok());
        assert!(validate_lineage_direction("../commands").is_err());
    }

    #[test] fn non_success_agent_status_never_becomes_a_receipt() {
        assert!(require_success_status(200).is_ok());
        assert!(require_success_status(201).is_ok());
        assert!(require_success_status(400).is_err());
        assert!(require_success_status(401).is_err());
        assert!(require_success_status(409).is_err());
        assert!(require_success_status(500).is_err());
    }

    #[test] fn agent_security_derive_and_adopt_requests_require_bounded_uuid_evidence() {
        let valid = r#"{"operationId":"11111111-1111-1111-1111-111111111111","sourceRevisionId":"22222222-2222-2222-2222-222222222222","name":"Derived"}"#;
        assert!(validate_derive_procedure_request(valid).is_ok());
        assert!(validate_derive_procedure_request(r#"{"operationId":"bad","sourceRevisionId":"22222222-2222-2222-2222-222222222222","name":"Derived"}"#).is_err());
        let adopt = r#"{"operationId":"11111111-1111-1111-1111-111111111111","procedureRevisionId":"22222222-2222-2222-2222-222222222222","expectedToken":0}"#;
        assert!(validate_adopt_procedure_request(adopt).is_ok());
        assert!(validate_adopt_procedure_request(r#"{"operationId":"11111111-1111-1111-1111-111111111111","procedureRevisionId":"bad","expectedToken":0}"#).is_err());
    }
    /// Live integration is intentionally excluded from ordinary cargo tests. The phase verifier
    /// selects it explicitly after starting its owned Agent/PostgreSQL host.
    #[test]
    #[ignore]
    fn agent_security_scientific_e2e() {
        assert_eq!(AGENT_HOST, "127.0.0.1");
        assert_eq!(AGENT_PORT, 5080);
        assert!(validate_json(r#"{"operationId":"00000000-0000-0000-0000-000000000001","name":"Tracer","components":[]}"#).is_ok());
    }
}
