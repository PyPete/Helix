// Helix Desktop — Tauri v2 application entry point
// Phase 5: config commands added; gRPC commands added in Phase 6

mod commands;
use commands::agent::{agent_approve_policy, agent_dispatch, agent_evidence, agent_propose, agent_status, bootstrap_agent_session, AgentBrokerState};
use commands::connection::{load_config, save_config};

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .manage(AgentBrokerState::new())
        .plugin(tauri_plugin_store::Builder::new().build())
        .plugin(tauri_plugin_fs::init())
        .invoke_handler(tauri::generate_handler![load_config, save_config, bootstrap_agent_session, agent_status, agent_propose, agent_approve_policy, agent_dispatch, agent_evidence])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
