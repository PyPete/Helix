import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/**
 * Manually curated sidebars for the Helix Orchestration documentation.
 *
 * Two top-level sections:
 *   - Developer Guide: architecture, adapters, transport, API reference
 *   - Operator Guide: workflow authoring, device setup, deployment
 */
const sidebars: SidebarsConfig = {
  developerSidebar: [
    {
      type: 'category',
      label: 'Developer Guide',
      items: [
        'developer/intro',
        // Future: developer/architecture, developer/adapters, developer/api-reference, etc.
      ],
    },
  ],

  operatorSidebar: [
    {
      type: 'category',
      label: 'Operator Guide',
      items: [
        'operator/intro',
        // Future: operator/workflow-authoring, operator/device-setup, operator/deployment, etc.
      ],
    },
  ],
};

export default sidebars;
