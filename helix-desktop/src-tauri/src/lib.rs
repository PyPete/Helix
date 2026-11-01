// Helix Desktop — Tauri v2 application entry point
// Phase 5: config commands added; gRPC commands added in Phase 6

mod commands;
use commands::agent::{agent_append_review_note, agent_approve_policy, agent_device_capabilities, agent_devices, agent_dispatch, agent_evidence, agent_evidence_cursor, agent_experiment_control, agent_experiment_start, agent_experiment_status, agent_logout, agent_operation_status, agent_propose, agent_revoke_session, agent_session_status, agent_status, bootstrap_agent_session, sample_append_event, sample_confirm, sample_events, sample_lineage, sample_list, sample_operation, sample_read, sample_transform, scientific_adopt_procedure, scientific_assemble, scientific_assembly_defaults, scientific_create, scientific_create_kind, scientific_derive_procedure, scientific_history, scientific_list, scientific_list_kind, scientific_operation, scientific_policies, scientific_publish, scientific_read, scientific_read_assembly, scientific_read_draft, scientific_read_policy, scientific_read_revision, scientific_save_draft, AgentBrokerState};
use commands::connection::{load_config, save_config};

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .manage(AgentBrokerState::new())
        .plugin(tauri_plugin_store::Builder::new().build())
        .plugin(tauri_plugin_fs::init())
        .invoke_handler(tauri::generate_handler![load_config, save_config, bootstrap_agent_session, agent_session_status, agent_logout, agent_revoke_session, agent_status, scientific_create, scientific_create_kind, scientific_derive_procedure, scientific_adopt_procedure, scientific_save_draft, scientific_publish, scientific_list, scientific_list_kind, scientific_read, scientific_read_draft, scientific_history, scientific_read_revision, scientific_operation, scientific_assembly_defaults, scientific_assemble, scientific_read_assembly, scientific_policies, scientific_read_policy, sample_list, sample_read, sample_confirm, sample_append_event, sample_events, sample_operation, sample_transform, sample_lineage, agent_devices, agent_device_capabilities, agent_propose, agent_approve_policy, agent_dispatch, agent_evidence, agent_operation_status, agent_evidence_cursor, agent_append_review_note, agent_experiment_status, agent_experiment_start, agent_experiment_control])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
