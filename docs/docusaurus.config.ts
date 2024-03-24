import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'BaGetter',
  tagline: 'Effortless .NET Core NuGet hosting',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
  url: 'https://www.bagetter.com/',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'bagetter', // Usually your GitHub org/user name.
  projectName: 'BaGetter', // Usually your repo name.

  deploymentBranch: "gh-pages",
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'throw',
  onBrokenAnchors: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/bagetter/BaGetter/tree/main/docs',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    docs: {
      sidebar: {
        hideable: true,
      }
    },
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    navbar: {
      title: 'BaGetter',
      logo: {
        alt: 'My Site Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/bagetter/BaGetter',
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
              label: 'BaGetter',
              to: '/docs',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'Stack Overflow',
              href: 'https://stackoverflow.com/questions/tagged/bagetter',
            },
            {
              label: 'Discord',
              href: 'https://discord.gg/XsAmm6f2hZ',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/bagetter/BaGetter',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} BaGetter. Built with <a href="https://www.docusaurus.io">Docusaurus</a>.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      //builtInLanguages: https://github.com/FormidableLabs/prism-react-renderer/blob/master/packages/generate-prism-languages/index.ts#L9
      //supportedLanguages: https://prismjs.com/#supported-languages
      additionalLanguages: ['csharp', 'json', 'powershell', 'javascript', 'bash'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
