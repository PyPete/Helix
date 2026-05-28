use serde::{Deserialize, Serialize};
use tauri::AppHandle;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AgentConnectionConfig {
    pub host: String,
    pub port: u16,
    pub protocol: String,
}

#[tauri::command]
pub async fn load_config(_app: AppHandle) -> Result<AgentConnectionConfig, String> {
    Ok(AgentConnectionConfig {
        host: "localhost".to_string(),
        port: 5080,
        protocol: "http".to_string(),
    })
}

#[tauri::command]
pub async fn save_config(
    _app: AppHandle,
    _config: AgentConnectionConfig,
) -> Result<(), String> {
    Ok(())
}
