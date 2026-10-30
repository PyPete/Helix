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
        'developer/architecture',
        'developer/adapter-authoring',
        'developer/transport-guide',
        'developer/rest-api',
        'developer/grpc-reference',
      ],
    },
  ],

  operatorSidebar: [
    {
      type: 'category',
      label: 'Operator Guide',
      items: [
        'operator/intro',
        'operator/workflow-authoring',
        'operator/device-capabilities',
        'operator/experiment-setup',
        'operator/deployment-setup',
        'operator/production-hardening',
      ],
    },
  ],
};

export default sidebars;
