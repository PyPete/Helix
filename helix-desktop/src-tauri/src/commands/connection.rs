use serde::Serialize;
use tauri::AppHandle;

/// A diagnostic only. The native broker owns the fixed target and session token.
#[derive(Debug, Clone, Serialize)]
pub struct LocalAgentConnectionStatus {
    pub target: String,
    pub legacy_settings_ignored: bool,
    pub migration_message: String,
}

#[tauri::command]
pub async fn load_config(_app: AppHandle) -> Result<LocalAgentConnectionStatus, String> {
    Ok(LocalAgentConnectionStatus {
        target: "127.0.0.1:5080".to_string(),
        legacy_settings_ignored: true,
        migration_message: "Legacy remote connection settings are ignored; restore the fixed local Agent session instead."
            .to_string(),
    })
}

/// Kept as an inert migration seam for old renderers. It accepts no target, credential, or actor.
#[tauri::command]
pub async fn save_config(_app: AppHandle) -> Result<LocalAgentConnectionStatus, String> {
    load_config(_app).await
}
