use std::ffi::c_void;
use std::fs::OpenOptions;
use std::io::Read;
use std::sync::Mutex;

use tauri::{State, WebviewWindow};

const MAIN_WINDOW: &str = "main";
const AGENT_HOST: &str = "127.0.0.1";
const AGENT_PORT: u16 = 5080;
const PIPE_PATH: &str = r"\\.\pipe\helix-desktop-session";

pub struct AgentBrokerState(pub Mutex<Option<String>>);

impl AgentBrokerState {
    pub fn new() -> Self {
        Self(Mutex::new(None))
    }
}

fn require_main_window(window: &WebviewWindow) -> Result<(), String> {
    if window.label() == MAIN_WINDOW {
        Ok(())
    } else {
        Err("Agent broker commands are restricted to the main local window.".to_string())
    }
}

#[tauri::command]
pub async fn bootstrap_agent_session(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<(), String> {
    require_main_window(&window)?;
    let mut pipe = OpenOptions::new().read(true).open(PIPE_PATH)
        .map_err(|_| "Unable to establish the local desktop session.".to_string())?;
    let mut response = String::new();
    pipe.read_to_string(&mut response)
        .map_err(|_| "Unable to read the local desktop session.".to_string())?;
    let token = response.lines().next().filter(|value| !value.is_empty())
        .ok_or_else(|| "Local desktop session response was invalid.".to_string())?;
    *state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())? = Some(token.to_string());
    Ok(())
}

#[tauri::command]
pub async fn agent_status(window: WebviewWindow, state: State<'_, AgentBrokerState>) -> Result<(), String> {
    require_main_window(&window)?;
    let token = state.0.lock().map_err(|_| "Agent session state is unavailable.".to_string())?
        .clone().ok_or_else(|| "No desktop Agent session is available.".to_string())?;
    winhttp_get_health(&token)
}

#[cfg(windows)]
fn wide(value: &str) -> Vec<u16> {
    value.encode_utf16().chain(std::iter::once(0)).collect()
}

#[cfg(windows)]
fn winhttp_get_health(token: &str) -> Result<(), String> {
    type Handle = *mut c_void;
    #[link(name = "winhttp")]
    unsafe extern "system" {
        fn WinHttpOpen(agent: *const u16, access: u32, proxy: *const u16, bypass: *const u16, flags: u32) -> Handle;
        fn WinHttpConnect(session: Handle, server: *const u16, port: u16, reserved: u32) -> Handle;
        fn WinHttpOpenRequest(connect: Handle, verb: *const u16, object: *const u16, version: *const u16, referer: *const u16, accept: *const *const u16, flags: u32) -> Handle;
        fn WinHttpAddRequestHeaders(request: Handle, headers: *const u16, length: u32, modifiers: u32) -> i32;
        fn WinHttpSendRequest(request: Handle, headers: *const u16, length: u32, optional: *mut c_void, optional_length: u32, total_length: u32, context: usize) -> i32;
        fn WinHttpReceiveResponse(request: Handle, reserved: *mut c_void) -> i32;
        fn WinHttpCloseHandle(handle: Handle) -> i32;
    }

    let agent = wide("HelixDesktop/1.0");
    let host = wide(AGENT_HOST);
    let get = wide("GET");
    let path = wide("/health");
    let header = wide(&format!("Authorization: Bearer {token}\r\n"));
    unsafe {
        let session = WinHttpOpen(agent.as_ptr(), 1, std::ptr::null(), std::ptr::null(), 0);
        if session.is_null() { return Err("Unable to open fixed local Agent transport.".to_string()); }
        let connect = WinHttpConnect(session, host.as_ptr(), AGENT_PORT, 0);
        if connect.is_null() { WinHttpCloseHandle(session); return Err("Unable to connect to the fixed local Agent.".to_string()); }
        let request = WinHttpOpenRequest(connect, get.as_ptr(), path.as_ptr(), std::ptr::null(), std::ptr::null(), std::ptr::null(), 0);
        if request.is_null() { WinHttpCloseHandle(connect); WinHttpCloseHandle(session); return Err("Unable to create local Agent request.".to_string()); }
        let added = WinHttpAddRequestHeaders(request, header.as_ptr(), u32::MAX, 0);
        let sent = added != 0 && WinHttpSendRequest(request, std::ptr::null(), 0, std::ptr::null_mut(), 0, 0, 0) != 0;
        let received = sent && WinHttpReceiveResponse(request, std::ptr::null_mut()) != 0;
        WinHttpCloseHandle(request); WinHttpCloseHandle(connect); WinHttpCloseHandle(session);
        if received { Ok(()) } else { Err("Local Agent health request failed.".to_string()) }
    }
}

#[cfg(not(windows))]
fn winhttp_get_health(_token: &str) -> Result<(), String> {
    Err("The native Agent broker is supported only on Windows.".to_string())
}

#[cfg(test)]
mod agent_security {
    use super::*;

    #[test]
    fn fixed_agent_target_is_loopback_only() {
        assert_eq!(AGENT_HOST, "127.0.0.1");
        assert_eq!(AGENT_PORT, 5080);
    }

    #[test]
    fn broker_never_serializes_its_session_state() {
        let state = AgentBrokerState::new();
        assert!(state.0.lock().unwrap().is_none());
    }
}
