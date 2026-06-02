import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'Helix Orchestration',
  tagline: 'Lab automation platform for flow chemistry',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true,
  },

  // GitHub Pages deployment URL
  url: 'https://PyPete.github.io',
  baseUrl: '/Helix/',

  // GitHub pages deployment config
  organizationName: 'PyPete',
  projectName: 'Helix',
  deploymentBranch: 'gh-pages',
  trailingSlash: false,

  onBrokenLinks: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  markdown: {
    mermaid: true,
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },

  themes: ['@docusaurus/theme-mermaid'],

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/PyPete/Helix/tree/main/docs-site/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Helix Orchestration',
      logo: {
        alt: 'Helix Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          to: '/docs/developer/intro',
          position: 'left',
          label: 'Developer Guide',
        },
        {
          to: '/docs/operator/intro',
          position: 'left',
          label: 'Operator Guide',
        },
        {
          href: 'https://github.com/PyPete/Helix',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Developer Guide',
              to: '/docs/developer/intro',
            },
            {
              label: 'Operator Guide',
              to: '/docs/operator/intro',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/PyPete/Helix',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Helix Orchestration. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
