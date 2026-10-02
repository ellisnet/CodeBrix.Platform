/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Runtime.Serialization;

namespace CodeBrix.Platform.PlayTest; //was previously: Microsoft.Playwright;

/// <summary>
/// WAI-ARIA roles accepted by <see cref="Page.GetByRole"/> and <see cref="Locator.GetByRole"/>. The
/// full set is kept so test code reads like its browser counterpart. PlayTest assigns these roles
/// to desktop controls (directly or through their automation peers): Button, Checkbox, Combobox, Dialog,
/// Group, Img, Link, Listbox, Menu, Menubar, Menuitem, Menuitemcheckbox, Option, Progressbar,
/// Radio, Separator, Slider, Switch, Tab, Tablist, Textbox, Toolbar, Tree and Treeitem. Every
/// other element reports <see cref="Generic"/>.
/// </summary>
public enum AriaRole
{
    /// <summary>The ARIA <c>alert</c> role.</summary>
    [EnumMember(Value = "alert")]
    Alert,
    /// <summary>The ARIA <c>alertdialog</c> role.</summary>
    [EnumMember(Value = "alertdialog")]
    Alertdialog,
    /// <summary>The ARIA <c>application</c> role.</summary>
    [EnumMember(Value = "application")]
    Application,
    /// <summary>The ARIA <c>article</c> role.</summary>
    [EnumMember(Value = "article")]
    Article,
    /// <summary>The ARIA <c>banner</c> role.</summary>
    [EnumMember(Value = "banner")]
    Banner,
    /// <summary>The ARIA <c>blockquote</c> role.</summary>
    [EnumMember(Value = "blockquote")]
    Blockquote,
    /// <summary>The ARIA <c>button</c> role.</summary>
    [EnumMember(Value = "button")]
    Button,
    /// <summary>The ARIA <c>caption</c> role.</summary>
    [EnumMember(Value = "caption")]
    Caption,
    /// <summary>The ARIA <c>cell</c> role.</summary>
    [EnumMember(Value = "cell")]
    Cell,
    /// <summary>The ARIA <c>checkbox</c> role.</summary>
    [EnumMember(Value = "checkbox")]
    Checkbox,
    /// <summary>The ARIA <c>code</c> role.</summary>
    [EnumMember(Value = "code")]
    Code,
    /// <summary>The ARIA <c>columnheader</c> role.</summary>
    [EnumMember(Value = "columnheader")]
    Columnheader,
    /// <summary>The ARIA <c>combobox</c> role.</summary>
    [EnumMember(Value = "combobox")]
    Combobox,
    /// <summary>The ARIA <c>complementary</c> role.</summary>
    [EnumMember(Value = "complementary")]
    Complementary,
    /// <summary>The ARIA <c>contentinfo</c> role.</summary>
    [EnumMember(Value = "contentinfo")]
    Contentinfo,
    /// <summary>The ARIA <c>definition</c> role.</summary>
    [EnumMember(Value = "definition")]
    Definition,
    /// <summary>The ARIA <c>deletion</c> role.</summary>
    [EnumMember(Value = "deletion")]
    Deletion,
    /// <summary>The ARIA <c>dialog</c> role.</summary>
    [EnumMember(Value = "dialog")]
    Dialog,
    /// <summary>The ARIA <c>directory</c> role.</summary>
    [EnumMember(Value = "directory")]
    Directory,
    /// <summary>The ARIA <c>document</c> role.</summary>
    [EnumMember(Value = "document")]
    Document,
    /// <summary>The ARIA <c>emphasis</c> role.</summary>
    [EnumMember(Value = "emphasis")]
    Emphasis,
    /// <summary>The ARIA <c>feed</c> role.</summary>
    [EnumMember(Value = "feed")]
    Feed,
    /// <summary>The ARIA <c>figure</c> role.</summary>
    [EnumMember(Value = "figure")]
    Figure,
    /// <summary>The ARIA <c>form</c> role.</summary>
    [EnumMember(Value = "form")]
    Form,
    /// <summary>The ARIA <c>generic</c> role.</summary>
    [EnumMember(Value = "generic")]
    Generic,
    /// <summary>The ARIA <c>grid</c> role.</summary>
    [EnumMember(Value = "grid")]
    Grid,
    /// <summary>The ARIA <c>gridcell</c> role.</summary>
    [EnumMember(Value = "gridcell")]
    Gridcell,
    /// <summary>The ARIA <c>group</c> role.</summary>
    [EnumMember(Value = "group")]
    Group,
    /// <summary>The ARIA <c>heading</c> role.</summary>
    [EnumMember(Value = "heading")]
    Heading,
    /// <summary>The ARIA <c>img</c> role.</summary>
    [EnumMember(Value = "img")]
    Img,
    /// <summary>The ARIA <c>insertion</c> role.</summary>
    [EnumMember(Value = "insertion")]
    Insertion,
    /// <summary>The ARIA <c>link</c> role.</summary>
    [EnumMember(Value = "link")]
    Link,
    /// <summary>The ARIA <c>list</c> role.</summary>
    [EnumMember(Value = "list")]
    List,
    /// <summary>The ARIA <c>listbox</c> role.</summary>
    [EnumMember(Value = "listbox")]
    Listbox,
    /// <summary>The ARIA <c>listitem</c> role.</summary>
    [EnumMember(Value = "listitem")]
    Listitem,
    /// <summary>The ARIA <c>log</c> role.</summary>
    [EnumMember(Value = "log")]
    Log,
    /// <summary>The ARIA <c>main</c> role.</summary>
    [EnumMember(Value = "main")]
    Main,
    /// <summary>The ARIA <c>marquee</c> role.</summary>
    [EnumMember(Value = "marquee")]
    Marquee,
    /// <summary>The ARIA <c>math</c> role.</summary>
    [EnumMember(Value = "math")]
    Math,
    /// <summary>The ARIA <c>meter</c> role.</summary>
    [EnumMember(Value = "meter")]
    Meter,
    /// <summary>The ARIA <c>menu</c> role.</summary>
    [EnumMember(Value = "menu")]
    Menu,
    /// <summary>The ARIA <c>menubar</c> role.</summary>
    [EnumMember(Value = "menubar")]
    Menubar,
    /// <summary>The ARIA <c>menuitem</c> role.</summary>
    [EnumMember(Value = "menuitem")]
    Menuitem,
    /// <summary>The ARIA <c>menuitemcheckbox</c> role.</summary>
    [EnumMember(Value = "menuitemcheckbox")]
    Menuitemcheckbox,
    /// <summary>The ARIA <c>menuitemradio</c> role.</summary>
    [EnumMember(Value = "menuitemradio")]
    Menuitemradio,
    /// <summary>The ARIA <c>navigation</c> role.</summary>
    [EnumMember(Value = "navigation")]
    Navigation,
    /// <summary>The ARIA <c>none</c> role.</summary>
    [EnumMember(Value = "none")]
    None,
    /// <summary>The ARIA <c>note</c> role.</summary>
    [EnumMember(Value = "note")]
    Note,
    /// <summary>The ARIA <c>option</c> role.</summary>
    [EnumMember(Value = "option")]
    Option,
    /// <summary>The ARIA <c>paragraph</c> role.</summary>
    [EnumMember(Value = "paragraph")]
    Paragraph,
    /// <summary>The ARIA <c>presentation</c> role.</summary>
    [EnumMember(Value = "presentation")]
    Presentation,
    /// <summary>The ARIA <c>progressbar</c> role.</summary>
    [EnumMember(Value = "progressbar")]
    Progressbar,
    /// <summary>The ARIA <c>radio</c> role.</summary>
    [EnumMember(Value = "radio")]
    Radio,
    /// <summary>The ARIA <c>radiogroup</c> role.</summary>
    [EnumMember(Value = "radiogroup")]
    Radiogroup,
    /// <summary>The ARIA <c>region</c> role.</summary>
    [EnumMember(Value = "region")]
    Region,
    /// <summary>The ARIA <c>row</c> role.</summary>
    [EnumMember(Value = "row")]
    Row,
    /// <summary>The ARIA <c>rowgroup</c> role.</summary>
    [EnumMember(Value = "rowgroup")]
    Rowgroup,
    /// <summary>The ARIA <c>rowheader</c> role.</summary>
    [EnumMember(Value = "rowheader")]
    Rowheader,
    /// <summary>The ARIA <c>scrollbar</c> role.</summary>
    [EnumMember(Value = "scrollbar")]
    Scrollbar,
    /// <summary>The ARIA <c>search</c> role.</summary>
    [EnumMember(Value = "search")]
    Search,
    /// <summary>The ARIA <c>searchbox</c> role.</summary>
    [EnumMember(Value = "searchbox")]
    Searchbox,
    /// <summary>The ARIA <c>separator</c> role.</summary>
    [EnumMember(Value = "separator")]
    Separator,
    /// <summary>The ARIA <c>slider</c> role.</summary>
    [EnumMember(Value = "slider")]
    Slider,
    /// <summary>The ARIA <c>spinbutton</c> role.</summary>
    [EnumMember(Value = "spinbutton")]
    Spinbutton,
    /// <summary>The ARIA <c>status</c> role.</summary>
    [EnumMember(Value = "status")]
    Status,
    /// <summary>The ARIA <c>strong</c> role.</summary>
    [EnumMember(Value = "strong")]
    Strong,
    /// <summary>The ARIA <c>subscript</c> role.</summary>
    [EnumMember(Value = "subscript")]
    Subscript,
    /// <summary>The ARIA <c>superscript</c> role.</summary>
    [EnumMember(Value = "superscript")]
    Superscript,
    /// <summary>The ARIA <c>switch</c> role.</summary>
    [EnumMember(Value = "switch")]
    Switch,
    /// <summary>The ARIA <c>tab</c> role.</summary>
    [EnumMember(Value = "tab")]
    Tab,
    /// <summary>The ARIA <c>table</c> role.</summary>
    [EnumMember(Value = "table")]
    Table,
    /// <summary>The ARIA <c>tablist</c> role.</summary>
    [EnumMember(Value = "tablist")]
    Tablist,
    /// <summary>The ARIA <c>tabpanel</c> role.</summary>
    [EnumMember(Value = "tabpanel")]
    Tabpanel,
    /// <summary>The ARIA <c>term</c> role.</summary>
    [EnumMember(Value = "term")]
    Term,
    /// <summary>The ARIA <c>textbox</c> role.</summary>
    [EnumMember(Value = "textbox")]
    Textbox,
    /// <summary>The ARIA <c>time</c> role.</summary>
    [EnumMember(Value = "time")]
    Time,
    /// <summary>The ARIA <c>timer</c> role.</summary>
    [EnumMember(Value = "timer")]
    Timer,
    /// <summary>The ARIA <c>toolbar</c> role.</summary>
    [EnumMember(Value = "toolbar")]
    Toolbar,
    /// <summary>The ARIA <c>tooltip</c> role.</summary>
    [EnumMember(Value = "tooltip")]
    Tooltip,
    /// <summary>The ARIA <c>tree</c> role.</summary>
    [EnumMember(Value = "tree")]
    Tree,
    /// <summary>The ARIA <c>treegrid</c> role.</summary>
    [EnumMember(Value = "treegrid")]
    Treegrid,
    /// <summary>The ARIA <c>treeitem</c> role.</summary>
    [EnumMember(Value = "treeitem")]
    Treeitem,
}
