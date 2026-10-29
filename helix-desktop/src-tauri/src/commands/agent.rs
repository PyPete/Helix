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
pub async fn agent_evidence_cursor(window: WebviewWindow, state: State<'_, AgentBrokerState>, cursor: Option<String>, page_size: u16) -> Result<String, String> {
    require_main_window(&window)?;
    if !(1..=100).contains(&page_size) {
        return Err("Evidence page size is invalid.".to_string());
    }
    let cursor = cursor.unwrap_or_default();
    validate_cursor(&cursor)?;
    let path = format!("/commands/evidence?cursor={cursor}&pageSize={page_size}");
    native_request("GET", &path, None, &require_session(&state)?)
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
pub async fn agent_experiment_control(window: WebviewWindow, state: State<'_, AgentBrokerState>, experiment_id: String, control: String, operation_id: String) -> Result<(), String> {
    require_main_window(&window)?;
    validate_command_id(&experiment_id)?;
    validate_command_id(&operation_id)?;
    if !matches!(control.as_str(), "pause" | "resume" | "abort") {
        return Err("Experiment control is invalid.".to_string());
    }
    let body = serde_json::json!({ "operationId": operation_id }).to_string();
    native_request("POST", &format!("/experiments/{experiment_id}/{control}"), Some(&body), &require_session(&state)?)?;
    Ok(())
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
        if session.is_null() { return Err("Unable to open fixed local Agent transport.".to_string()); }
        let disable_redirects = WINHTTP_DISABLE_REDIRECTS;
        if WinHttpSetOption(
            session,
            WINHTTP_OPTION_DISABLE_FEATURE,
            &disable_redirects as *const u32 as *mut c_void,
            std::mem::size_of::<u32>() as u32,
        ) == 0 || WinHttpSetTimeouts(session, 5_000, 5_000, 5_000, 5_000) == 0 {
            WinHttpCloseHandle(session);
            return Err("Unable to secure the fixed local Agent transport.".to_string());
        }
        let connect = WinHttpConnect(session, host.as_ptr(), AGENT_PORT, 0);
        if connect.is_null() { WinHttpCloseHandle(session); return Err("Unable to connect to the fixed local Agent.".to_string()); }
        let request = WinHttpOpenRequest(connect, verb.as_ptr(), path.as_ptr(), std::ptr::null(), std::ptr::null(), std::ptr::null(), 0);
        if request.is_null() { WinHttpCloseHandle(connect); WinHttpCloseHandle(session); return Err("Unable to create local Agent request.".to_string()); }
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
    #[test] fn fixed_agent_target_is_loopback_only() { assert_eq!(AGENT_HOST, "127.0.0.1"); assert_eq!(AGENT_PORT, 5080); }
    #[test] fn broker_never_serializes_its_session_state() { assert!(AgentBrokerState::new().0.lock().unwrap().session.is_none()); }
    #[test] fn command_ids_do_not_accept_paths() { assert!(validate_command_id("../commands").is_err()); }
    #[test] fn command_ids_require_uuid_shape() { assert!(validate_command_id("------------------------------------").is_err()); }
    #[test] fn cursor_does_not_accept_query_injection() { assert!(validate_cursor("cursor&host=remote").is_err()); }
}
