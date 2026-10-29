use std::ffi::c_void;
use std::fs::OpenOptions;
use std::io::Read;
use std::sync::Mutex;

use tauri::{State, WebviewWindow};

const MAIN_WINDOW: &str = "main";
const AGENT_HOST: &str = "127.0.0.1";
const AGENT_PORT: u16 = 5080;
const PIPE_PATH: &str = r"\\.\pipe\helix-desktop-session";
const MAX_REQUEST_BYTES: usize = 4 * 1024;

pub struct AgentBrokerState(pub Mutex<Option<String>>);

impl AgentBrokerState {
    pub fn new() -> Self { Self(Mutex::new(None)) }
}

fn require_main_window(window: &WebviewWindow) -> Result<(), String> {
    if window.label() == MAIN_WINDOW { Ok(()) }
    else { Err("Agent broker commands are restricted to the main local window.".to_string()) }
}

fn require_session(state: &State<'_, AgentBrokerState>) -> Result<String, String> {
    state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())?
        .clone().ok_or_else(|| "No desktop Agent session is available.".to_string())
}

fn validate_json(value: &str) -> Result<(), String> {
    if value.len() > MAX_REQUEST_BYTES { return Err("Agent request exceeds the permitted size.".to_string()); }
    serde_json::from_str::<serde_json::Value>(value)
        .map(|_| ()).map_err(|_| "Agent request must be valid JSON.".to_string())
}

fn validate_command_id(command_id: &str) -> Result<(), String> {
    if command_id.len() != 36 || !command_id.bytes().all(|byte| byte.is_ascii_hexdigit() || byte == b'-') {
        return Err("Command identifier is invalid.".to_string());
    }
    Ok(())
}

#[tauri::command]
pub async fn bootstrap_agent_session(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<(), String> {
    require_main_window(&window)?;
    let mut pipe = OpenOptions::new().read(true).open(PIPE_PATH)
        .map_err(|_| "Unable to establish the local desktop session.".to_string())?;
    let mut response = String::new();
    pipe.read_to_string(&mut response).map_err(|_| "Unable to read the local desktop session.".to_string())?;
    let token = response.lines().next().filter(|value| !value.is_empty())
        .ok_or_else(|| "Local desktop session response was invalid.".to_string())?;
    *state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())? = Some(token.to_string());
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

#[cfg(windows)]
fn wide(value: &str) -> Vec<u16> { value.encode_utf16().chain(std::iter::once(0)).collect() }

#[cfg(windows)]
fn native_request(verb: &str, path: &str, body: Option<&str>, token: &str) -> Result<String, String> {
    type Handle = *mut c_void;
    #[link(name = "winhttp")]
    unsafe extern "system" {
        fn WinHttpOpen(agent: *const u16, access: u32, proxy: *const u16, bypass: *const u16, flags: u32) -> Handle;
        fn WinHttpConnect(session: Handle, server: *const u16, port: u16, reserved: u32) -> Handle;
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
        let session = WinHttpOpen(user_agent.as_ptr(), 1, std::ptr::null(), std::ptr::null(), 0);
        if session.is_null() { return Err("Unable to open fixed local Agent transport.".to_string()); }
        let connect = WinHttpConnect(session, host.as_ptr(), AGENT_PORT, 0);
        if connect.is_null() { WinHttpCloseHandle(session); return Err("Unable to connect to the fixed local Agent.".to_string()); }
        let request = WinHttpOpenRequest(connect, verb.as_ptr(), path.as_ptr(), std::ptr::null(), std::ptr::null(), std::ptr::null(), 0);
        if request.is_null() { WinHttpCloseHandle(connect); WinHttpCloseHandle(session); return Err("Unable to create local Agent request.".to_string()); }
        let sent = WinHttpAddRequestHeaders(request, headers.as_ptr(), u32::MAX, 0) != 0 &&
            WinHttpSendRequest(request, std::ptr::null(), 0, payload.as_ptr() as *mut c_void, payload.len() as u32, payload.len() as u32, 0) != 0;
        let received = sent && WinHttpReceiveResponse(request, std::ptr::null_mut()) != 0;
        if !received { WinHttpCloseHandle(request); WinHttpCloseHandle(connect); WinHttpCloseHandle(session); return Err("Local Agent request failed.".to_string()); }
        let mut response = Vec::new();
        loop {
            let mut available = 0u32;
            if WinHttpQueryDataAvailable(request, &mut available) == 0 { break; }
            if available == 0 { break; }
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
    #[test] fn broker_never_serializes_its_session_state() { assert!(AgentBrokerState::new().0.lock().unwrap().is_none()); }
    #[test] fn command_ids_do_not_accept_paths() { assert!(validate_command_id("../commands").is_err()); }
}
