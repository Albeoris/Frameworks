using System.Text;
using System.Text.Json;
using Albeoris.Games.FF8.Toolset.Analysis.Model;
using Scriban;
using Scriban.Runtime;

namespace Albeoris.Games.FF8.Toolset.Analysis.Reports;

internal sealed class HtmlAnalysisReportFormatter : IAnalysisReportFormatter
{
    private readonly JsonSerializerOptions jsonOptions = AnalysisJsonSerializerOptions.Create(indented: false);
    private readonly Template template;

    public HtmlAnalysisReportFormatter()
    {
        template = Template.Parse(HtmlTemplate);
        if (template.HasErrors)
            throw new InvalidOperationException($"The HTML report template is invalid: {template.Messages}");
    }

    public async Task WriteAsync(
        AnalysisReport report,
        String outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        String reportJson = JsonSerializer.Serialize(report, jsonOptions);
        TemplateContext context = new() { LimitToString = Int32.MaxValue };
        context.PushGlobal(new ScriptObject { ["report_json"] = reportJson });
        String html = template.Render(context);
        await File.WriteAllTextAsync(outputPath, html, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private const String HtmlTemplate = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Final Fantasy VIII analysis</title>
<style>
:root { color-scheme: light dark; font-family: Inter, "Segoe UI", system-ui, sans-serif; }
* { box-sizing: border-box; }
body { margin: 0; background: #0f1117; color: #e7eaf0; }
.page { margin: 0 auto; max-width: 1600px; padding: 1.5rem; }
h1 { font-size: 1.55rem; margin: 0 0 .3rem; }
h2 { font-size: 1.05rem; margin: 1.5rem 0 .55rem; }
.meta, .result-count { color: #9299a8; font-size: .88rem; }
.layout { display: grid; gap: 1.25rem; grid-template-columns: minmax(0, 1fr) 280px; margin-top: 1.2rem; }
.toolbar { align-items: end; background: #171a22; border: 1px solid #2a2f3b; border-radius: .65rem; display: grid; gap: .75rem; grid-template-columns: 1.3fr 1fr 1fr auto; padding: .85rem; }
label { color: #b9bfca; display: grid; font-size: .82rem; font-weight: 600; gap: .3rem; }
input { background: #10131a; border: 1px solid #353b49; border-radius: .38rem; color: inherit; font: inherit; min-width: 0; padding: .52rem .65rem; }
input:focus { border-color: #67a2ff; outline: 2px solid #67a2ff33; }
button { font: inherit; }
.apply { background: #3979d6; border: 0; border-radius: .4rem; color: white; cursor: pointer; font-weight: 650; padding: .56rem 1rem; }
.apply:hover { background: #4a8aeb; }
.sidebar { align-self: start; background: #171a22; border: 1px solid #2a2f3b; border-radius: .65rem; max-height: calc(100vh - 3rem); overflow: auto; padding: .8rem; position: sticky; top: 1rem; }
.facet { border: 0; border-top: 1px solid #2a2f3b; margin: .65rem 0 0; padding: .75rem 0 0; }
.facet:first-child { border-top: 0; margin-top: 0; padding-top: 0; }
.facet legend { font-size: .82rem; font-weight: 700; padding: 0; }
.facet-tools { display: flex; gap: .4rem; margin: .35rem 0 .45rem; }
.facet-tools button { background: transparent; border: 0; color: #79aef8; cursor: pointer; font-size: .75rem; padding: 0; }
.checks { display: grid; gap: .3rem; max-height: 15rem; overflow: auto; }
.check { align-items: center; color: #c7ccd5; display: flex; font-size: .78rem; font-weight: 400; gap: .42rem; }
.check input { accent-color: #4f91ee; margin: 0; min-width: auto; }
.tree-panel { background: #14171e; border: 1px solid #272c37; border-radius: .6rem; min-height: 3rem; padding: .5rem; }
.tree-node { --depth: 0; }
.node-row { align-items: center; border-radius: .32rem; display: flex; gap: .3rem; min-height: 1.85rem; padding: .16rem .35rem .16rem calc(.25rem + var(--depth) * 1.15rem); }
.node-row:hover, .node-row:focus-within { background: #232833; }
.toggle, .toggle-space { align-items: center; display: inline-flex; flex: 0 0 1.15rem; height: 1.15rem; justify-content: center; }
.toggle { background: transparent; border: 0; border-radius: .25rem; color: #8f98a9; cursor: pointer; font-size: 1.12rem; line-height: 1; padding: 0; transition: transform .12s ease; }
.toggle[aria-expanded="true"] { transform: rotate(90deg); }
.toggle:hover { background: #ffffff12; color: #dce2ed; }
.node-icon { flex: 0 0 1.25rem; font-size: .95rem; text-align: center; }
.node-name { color: #dce1ea; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.node-meta { color: #7f8899; font-size: .75rem; white-space: nowrap; }
.category { background: #34415b; border-radius: 999px; color: #cddaf1; font-size: .68rem; padding: .08rem .38rem; white-space: nowrap; }
.node-actions { display: flex; gap: .15rem; margin-left: auto; opacity: 0; }
.node-row:hover .node-actions, .node-row:focus-within .node-actions { opacity: 1; }
.node-action { background: transparent; border: 0; border-radius: .25rem; color: #aeb6c4; cursor: pointer; padding: .12rem .32rem; }
.node-action:hover { background: #ffffff14; color: white; }
.children[hidden] { display: none; }
.empty { color: #7f8795; font-style: italic; margin: .6rem; }
@media (max-width: 980px) {
  .layout { grid-template-columns: 1fr; }
  .sidebar { max-height: none; position: static; }
  .toolbar { grid-template-columns: 1fr 1fr; }
}
</style>
</head>
<body>
<main class="page">
  <h1>Final Fantasy VIII analysis</h1>
  <div class="meta" id="meta"></div>
  <form class="toolbar" id="filters">
    <label>Search<input id="search" type="search" placeholder="name, path, category"></label>
    <label>Include path patterns<input id="include" placeholder="*en/*, */kernel.bin"></label>
    <label>Exclude path patterns<input id="exclude" placeholder="*lang-jp*"></label>
    <button class="apply" type="submit">Apply filters</button>
  </form>
  <div class="layout">
    <section>
      <h2>Archive trees</h2>
      <div class="result-count" id="archive-count"></div>
      <div class="tree-panel" id="archives"></div>
      <h2>Translatable files</h2>
      <div class="result-count" id="translatable-count"></div>
      <div class="tree-panel" id="translatable"></div>
    </section>
    <aside class="sidebar" id="facets" aria-label="File filters"></aside>
  </div>
</main>
<script id="report-data" type="application/json">{{ report_json }}</script>
<script>
'use strict';
const report = JSON.parse(document.getElementById('report-data').textContent);
const elements = {
  form: document.getElementById('filters'), search: document.getElementById('search'),
  include: document.getElementById('include'), exclude: document.getElementById('exclude'),
  archives: document.getElementById('archives'), translatable: document.getElementById('translatable'),
  archiveCount: document.getElementById('archive-count'), translatableCount: document.getElementById('translatable-count'),
  facets: document.getElementById('facets')
};
const categoryLabels = {
  dialogues: 'Dialogues', systemTextAndUi: 'System text & UI', fonts: 'Fonts',
  japaneseFonts: 'Japanese fonts', textTextures: 'Text textures', battleText: 'Battle text',
  uncategorized: 'Uncategorized'
};
const languageLabels = { en: 'English', fr: 'French', it: 'Italian', de: 'German', es: 'Spanish', jp: 'Japanese', unknown: 'Not detected' };
const languagePattern = /(\b|_|-)(en|fr|it|de|es|jp)\b/i;

document.getElementById('meta').textContent = `${report.gamePath} · ${report.archives.length} archives · ${report.translatableFiles.length} translatable files · ${new Date(report.generatedAtUtc).toLocaleString()}`;

function normalized(path) { return path.replace(/\\/g, '/'); }
function extension(path) {
  const name = normalized(path).split('/').pop() || '';
  const index = name.lastIndexOf('.');
  return index > 0 ? name.slice(index).toLowerCase() : '(no extension)';
}
function language(path) { const match = normalized(path).match(languagePattern); return match ? match[2].toLowerCase() : 'unknown'; }
function categories(node) { return node.translationCategories?.length ? node.translationCategories : ['uncategorized']; }
function patterns(value) { return value.split(/[\n,;]+/).map(x => x.trim()).filter(Boolean); }
function glob(pattern) {
  const escaped = pattern.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/\*/g, '.*').replace(/\?/g, '[^/]');
  return new RegExp(`^${escaped}$`, 'i');
}

function visitFiles(nodes, action) {
  for (const node of nodes || []) {
    if (!node.children?.length) action(node);
    else visitFiles(node.children, action);
  }
}

const allTypes = new Set();
for (const archive of report.archives) visitFiles(archive.children, node => allTypes.add(extension(node.path)));
for (const file of report.translatableFiles) allTypes.add(extension(file.path));

function createFacet(id, title, values, labelFor) {
  const fieldset = document.createElement('fieldset');
  fieldset.className = 'facet';
  const legend = document.createElement('legend');
  legend.textContent = title;
  fieldset.append(legend);
  const tools = document.createElement('div');
  tools.className = 'facet-tools';
  for (const [caption, checked] of [['All', true], ['None', false]]) {
    const button = document.createElement('button');
    button.type = 'button'; button.textContent = caption;
    button.addEventListener('click', () => fieldset.querySelectorAll('input').forEach(input => input.checked = checked));
    tools.append(button);
  }
  fieldset.append(tools);
  const checks = document.createElement('div');
  checks.className = 'checks';
  for (const value of values) {
    const label = document.createElement('label'); label.className = 'check';
    const input = document.createElement('input');
    input.type = 'checkbox'; input.checked = true; input.value = value; input.dataset.facet = id;
    label.append(input, document.createTextNode(labelFor(value)));
    checks.append(label);
  }
  fieldset.append(checks);
  elements.facets.append(fieldset);
}

createFacet('category', 'Categories', Object.keys(categoryLabels), value => categoryLabels[value]);
createFacet('type', 'File types', [...allTypes].sort(), value => value);
createFacet('language', 'Languages', Object.keys(languageLabels), value => languageLabels[value]);

function selectedFacet(id) {
  const inputs = [...document.querySelectorAll(`input[data-facet="${id}"]`)];
  return { selected: new Set(inputs.filter(x => x.checked).map(x => x.value)), total: inputs.length };
}

function currentFilter() {
  const filter = {
    term: elements.search.value.trim().toLowerCase(),
    includes: patterns(elements.include.value).map(glob), excludes: patterns(elements.exclude.value).map(glob),
    category: selectedFacet('category'), type: selectedFacet('type'), language: selectedFacet('language')
  };
  filter.fileFacetActive = [filter.category, filter.type, filter.language].some(facet => facet.selected.size !== facet.total);
  filter.active = Boolean(filter.term || filter.includes.length || filter.excludes.length || filter.fileFacetActive);
  return filter;
}

function facetAllows(facet, values) { return facet.selected.size === facet.total || values.some(value => facet.selected.has(value)); }
function pathAllows(path, filter) {
  const value = normalized(path);
  return (!filter.includes.length || filter.includes.some(x => x.test(value))) && !filter.excludes.some(x => x.test(value));
}
function fileAllows(node, fullPath, filter) {
  return pathAllows(fullPath, filter) &&
    facetAllows(filter.category, categories(node)) &&
    facetAllows(filter.type, [extension(node.path)]) &&
    facetAllows(filter.language, [language(fullPath)]);
}
function textMatches(node, fullPath, filter) {
  if (!filter.term) return true;
  const categoryText = categories(node).flatMap(value => [value, categoryLabels[value] || value]).join(' ');
  return `${node.name} ${fullPath} ${categoryText} ${extension(node.path)} ${language(fullPath)}`.toLowerCase().includes(filter.term);
}

function filterNode(node, rootPath, filter) {
  const fullPath = `${rootPath}/${node.path}`;
  const sourceChildren = node.children || [];
  const children = sourceChildren.map(child => filterNode(child, rootPath, filter)).filter(Boolean);
  const leaf = sourceChildren.length === 0;
  const selfMatch = textMatches(node, fullPath, filter);
  const visible = leaf
    ? fileAllows(node, fullPath, filter) && selfMatch
    : children.length > 0 || (!filter.fileFacetActive && selfMatch && pathAllows(fullPath, filter));
  if (!visible) return null;
  return { ...node, children, expand: Boolean(filter.term) && (selfMatch || children.some(child => child.expand)) };
}

function icon(node, open = false) {
  if (node.kind === 'archive') return '🗜️';
  if (node.children?.length) return open ? '📂' : '📁';
  const ext = extension(node.path);
  if (['.png', '.tex', '.tga', '.dds'].includes(ext)) return '🖼️';
  if (['.msd'].includes(ext)) return '💬';
  if (['.bin', '.dat', '.dc1', '.dc2'].includes(ext)) return '⚙️';
  if (['.ttf', '.otf', '.fnt'].includes(ext)) return '🔤';
  if (['.zzz', '.fl', '.fi', '.fs'].includes(ext)) return '🗜️';
  return '📄';
}

function treeNode(node, depth = 0) {
  const wrapper = document.createElement('div'); wrapper.className = 'tree-node'; wrapper.style.setProperty('--depth', depth);
  const row = document.createElement('div'); row.className = 'node-row'; row.title = node.path || node.name;
  const hasChildren = Boolean(node.children?.length);
  let toggle;
  if (hasChildren) {
    toggle = document.createElement('button'); toggle.className = 'toggle'; toggle.type = 'button'; toggle.textContent = '›'; toggle.setAttribute('aria-label', 'Toggle node');
  } else {
    toggle = document.createElement('span'); toggle.className = 'toggle-space';
  }
  const nodeIcon = document.createElement('span'); nodeIcon.className = 'node-icon'; nodeIcon.textContent = icon(node);
  const name = document.createElement('span'); name.className = 'node-name'; name.textContent = node.name;
  row.append(toggle, nodeIcon, name);
  if (!hasChildren && node.size != null) {
    const meta = document.createElement('span'); meta.className = 'node-meta'; meta.textContent = `${node.size} B`; row.append(meta);
  }
  for (const category of node.translationCategories || node.categories || []) {
    const badge = document.createElement('span'); badge.className = 'category'; badge.textContent = categoryLabels[category] || category; row.append(badge);
  }
  wrapper.append(row);
  if (!hasChildren) return wrapper;

  const actions = document.createElement('span'); actions.className = 'node-actions';
  const expand = document.createElement('button'); expand.type = 'button'; expand.className = 'node-action'; expand.textContent = '＋'; expand.title = 'Expand subtree';
  const collapse = document.createElement('button'); collapse.type = 'button'; collapse.className = 'node-action'; collapse.textContent = '−'; collapse.title = 'Collapse subtree';
  actions.append(expand, collapse); row.append(actions);
  const children = document.createElement('div'); children.className = 'children'; children.hidden = true; wrapper.append(children);
  let childElements = null;
  function ensureChildren() {
    if (childElements) return childElements;
    childElements = node.children.map(child => treeNode(child, depth + 1));
    children.append(...childElements);
    return childElements;
  }
  function setOpen(open) {
    toggle.setAttribute('aria-expanded', String(open)); children.hidden = !open; nodeIcon.textContent = icon(node, open);
    if (open) ensureChildren();
  }
  function setSubtree(open) {
    setOpen(open);
    if (open) {
      for (const child of ensureChildren()) child.setSubtree?.(true);
    } else if (childElements) {
      for (const child of childElements) child.setSubtree?.(false);
    } else {
      const markCollapsed = nodes => nodes.forEach(child => { child.expand = false; markCollapsed(child.children || []); });
      markCollapsed(node.children);
    }
  }
  wrapper.setSubtree = setSubtree;
  toggle.addEventListener('click', () => setOpen(toggle.getAttribute('aria-expanded') !== 'true'));
  expand.addEventListener('click', () => setSubtree(true));
  collapse.addEventListener('click', () => setSubtree(false));
  setOpen(Boolean(node.expand));
  return wrapper;
}

function translatableTree(files, expandPaths) {
  const root = { children: new Map() };
  for (const file of files) {
    const parts = normalized(file.path).split('/').filter(Boolean);
    let parent = root;
    parts.forEach((part, index) => {
      if (!parent.children.has(part)) parent.children.set(part, { name: part, path: parts.slice(0, index + 1).join('/'), children: new Map() });
      parent = parent.children.get(part);
      if (index === parts.length - 1) Object.assign(parent, { kind: 'file', size: file.size, categories: file.categories });
    });
  }
  function convert(node) {
    const children = [...node.children.values()].sort((a, b) => a.name.localeCompare(b.name)).map(convert);
    const archive = children.length && /\.(zzz|fl)$/i.test(node.name || '');
    return { ...node, kind: archive ? 'archive' : node.kind || 'folder', children, expand: expandPaths && children.length > 0 };
  }
  return [...root.children.values()].sort((a, b) => a.name.localeCompare(b.name)).map(convert);
}

function render() {
  const filter = currentFilter();
  elements.archives.replaceChildren();
  let archiveCount = 0;
  for (const archive of report.archives) {
    const children = archive.children.map(node => filterNode(node, archive.path, filter)).filter(Boolean);
    if (!children.length && filter.active) continue;
    const root = { name: archive.name, path: archive.path, kind: 'archive', size: archive.size, children, expand: Boolean(filter.term) && children.some(x => x.expand) };
    elements.archives.append(treeNode(root)); archiveCount++;
  }
  if (!archiveCount) elements.archives.innerHTML = '<p class="empty">No matching archives.</p>';
  elements.archiveCount.textContent = `${archiveCount} matching archive${archiveCount === 1 ? '' : 's'}`;

  const files = report.translatableFiles.filter(file => {
    const node = { name: normalized(file.path).split('/').pop(), path: file.path, translationCategories: file.categories };
    return fileAllows(node, file.path, filter) && textMatches(node, file.path, filter);
  });
  elements.translatable.replaceChildren();
  const roots = translatableTree(files, Boolean(filter.term));
  for (const root of roots) elements.translatable.append(treeNode(root));
  if (!roots.length) elements.translatable.innerHTML = '<p class="empty">No matching translatable files.</p>';
  elements.translatableCount.textContent = `${files.length} matching file${files.length === 1 ? '' : 's'}`;
}

elements.form.addEventListener('submit', event => { event.preventDefault(); render(); });
render();
</script>
</body>
</html>
""";
}
