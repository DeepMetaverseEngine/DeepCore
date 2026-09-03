using DeepCore.Components;
using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DeepCore.GUI.FairyGUI.Editor
{
    public interface UINode
    {
        UINode Parent { get; }
        string NodeName { get; }
        string DisplayName { get; }
    }

    //----------------------------------------------------------------------------------
    public class UIPackage : UINode
    {
        public readonly FileInfo _file;
        public string _name;
        public string id;
        public readonly HashMap<string, UIResource> resources = new();
        public UIPackage(FileInfo file)
        {
            this._file = file;
            this._name = file.Directory.Name;
        }
        public UINode Parent { get => null; }
        public string NodeName => _name;
        public string DisplayName => NodeName;
    }
    public class UIResource : UINode
    {
        public readonly string _element_name;
        public readonly UIPackage _pkg;
        public string path;
        public string id;
        public string name;

        public Vector2 size;
        public string overflow;
        public string opaque;
        public string designImage;
        public string designImageAlpha;
        public string bgColor;
        public string extention;

        public readonly ArrayList<UIController> controllers = new();
        public UIPackageItem component;

        public UIResource(UIPackage pkg, XmlElement e)
        {
            this._pkg = pkg;
            this._element_name = e.Name;
        }

        public UINode Parent { get => _pkg; }
        public string NodeName => Path.GetFileNameWithoutExtension(name);
        public string DisplayName => NodeName;
    }
    //----------------------------------------------------------------------------------

    public class UIPackageItem : UINode
    {
        public readonly string _element_name;
        public readonly UIResource _res;
        public UIPackageItem(UIResource res, XmlElement element)
        {
            this._res = res;
            this._element_name = element.Name;
        }

        public string id;
        public string name;
        public Vector2 xy;
        public Vector2 size;
        public string type;

        public string group;

        public string pkg;
        public string fileName;
        public string src;

        public string controller;
        public string blend;


        public readonly ListDictionary<string, UIPackageItem> displayList = new();
        public readonly ArrayList<UIPackageItem> treeView = new();

        public UIResource _src;

        public UINode Parent { get => _res; }
        public string NodeName => name;
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(fileName)) return name;
                return name + " {" + Path.GetFileNameWithoutExtension(fileName) + "}";
            }
        }
    }
    //----------------------------------------------------------------------------------
    public class UIController : UINode
    {
        public readonly UIPackageItem _item;
        public readonly string _element_name;
        public UIController(UIPackageItem item, XmlElement element)
        {
            this._item = item;
            this._element_name = element.Name;
        }
        public string name;
        public string homePageType;
        public string homePage;
        public readonly ArrayList<UIControllerPage> pages = new ArrayList<UIControllerPage>();
        public string selected = "";
        public UINode Parent { get => _item; }
        public string NodeName => name;
        public string DisplayName => NodeName;
    }

    public class UIControllerPage : UINode
    {
        public readonly UIController _controller;
        public UIControllerPage(UIController c, int index)
        {
            this._controller = c;
            this.index = index;
        }
        public readonly int index;
        public string name = string.Empty;
        public string remark = string.Empty;
        public UINode Parent { get => _controller; }
        public string NodeName => $"{index}:{name}:{remark}";
        public string DisplayName => NodeName;
    }

    //----------------------------------------------------------------------------------
    public class UIPackageLoader
    {
        private readonly DirectoryInfo assetRoot;
        private readonly ListDictionary<string, UIPackage> allPackages = new();
        private readonly ListDictionary<string, UIPackage> allPackagesF = new();
        private readonly Logger log;
        public UIPackageLoader(DirectoryInfo assetRoot)
        {
            this.assetRoot = assetRoot;
            this.log = LoggerFactory.GetLogger("FGUI");
        }
        public UIPackage[] Packages => allPackages.Values.ToArray();
        public void LoadAll()
        {
            foreach (var dir in assetRoot.GetDirectories())
            {
                var file = new FileInfo(Path.Combine(dir.FullName, "package.xml"));
                if (file.Exists)
                {
                    var pkg = LoadPackage(file);
                    try
                    {
                        allPackages.Add(pkg.id, pkg);
                        allPackagesF.Add(pkg._name, pkg);
                    }
                    catch (Exception err)
                    {
                        log.Error($"Load Package Error : {file}\n{err.Message}", err);
                    }
                }
            }
            foreach (var pkg in allPackages.Values)
            {
                foreach (var res in pkg.resources)
                {
                    if (res.Value.component != null)
                    {
                        LoadTreeView(res.Value.component);
                    }
                }
            }
        }
        public static XmlDocument LoadRawXML(FileInfo file)
        {
            var xmlbin = File.ReadAllBytes(file.FullName);
            if (xmlbin != null)
            {
                for (int i = 0; i < xmlbin.Length; i++)
                {
                    if (xmlbin[i] == 0)
                    {
                        xmlbin[i] = (byte)(' ');
                    }
                }
                try
                {
                    return XmlUtil.LoadXML(xmlbin);
                }
                catch (Exception err)
                {
                    err.PrintStackTrace($"LoadRawXML Error : {err.Message} : {file}");
                }
            }
            return null;
        }
        public bool TryGetPackageByName(string name, out UIPackage pkg)
        {
            return allPackagesF.TryGetValue(name, out pkg);
        }
        public bool TryGetPackage(string id, out UIPackage pkg)
        {
            return allPackages.TryGetValue(id, out pkg);
        }
        private UIPackage LoadPackage(FileInfo path)
        {
            var pkg = new UIPackage(path);
            try
            {
                XmlDocument xml = LoadRawXML(path);
                if (xml != null)
                {
                    var packageDescription = xml.DocumentElement;
                    pkg.id = packageDescription.GetAttribute("id");
                    var resources = packageDescription.GetXmlElement("resources");
                    foreach (XmlElement resource in resources)
                    {
                        var component = LoadResource(resource, pkg);
                        pkg.resources.Add(component.id, component);
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"LoadPackage Error : {err.Message} : {path}");
                throw;
            }
            return pkg;
        }
        private UIResource LoadResource(XmlElement e, UIPackage pkg)
        {
            var res = new UIResource(pkg, e)
            {
                id = e.GetAttribute("id"),
                name = e.GetAttribute("name"),
                path = e.GetAttribute("path"),
            };
            if (e.Name == "component")
            {
                var file = pkg._file.Directory.GetChildFile(Path.Combine(res.path, res.name));
                if (file != null)
                {
                    try
                    {
                        var xml = LoadRawXML(file);
                        if (xml != null)
                        {
                            var x = xml.DocumentElement;
                            var item = new UIPackageItem(res, x)
                            {
                                id = res.id,
                                name = res.name,
                                size = DecodeVector2(x.GetAttribute("size")),
                            };
                            {
                                res.size = DecodeVector2(x.GetAttribute("size"));
                                res.overflow = x.GetAttribute("overflow");
                                res.opaque = x.GetAttribute("opaque");
                                res.designImage = x.GetAttribute("designImage");
                                res.extention = x.GetAttribute("extention");
                            }
                            var displayList = x.GetXmlElement("displayList");
                            if (displayList != null)
                            {
                                displayList.ForEachChilds((XmlElement ee) =>
                                {
                                    var display = LoadDisplayElement(ee, res);
                                    item.displayList.Put(display.id, display);
                                });
                                foreach (var display in item.displayList.Values)
                                {
                                    if (string.IsNullOrEmpty(display.group))
                                    {
                                        item.treeView.Add(display);
                                    }
                                    else if (item.displayList.TryGetValue(display.group, out var parent))
                                    {
                                        parent.treeView.Add(display);
                                    }
                                    else
                                    {
                                        throw new Exception($"Group node not found : node={display.name} group={display.group}");
                                    }
                                }
                            }
                            x.ForEachChilds((XmlElement ee) =>
                            {
                                if (ee.Name == "controller")
                                {
                                    res.controllers.Add(LoadController(ee, item));
                                }
                            });
                            res.component = item;
                        }
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace($"LoadResource Error : {err.Message} : {file}");
                        throw;
                    }
                }
            }
            return res;
        }
        private UIPackageItem LoadDisplayElement(XmlElement e, UIResource res)
        {
            var c = new UIPackageItem(res, e)
            {
                id = e.GetAttribute("id"),
                name = e.GetAttribute("name"),
                xy = DecodeVector2(e.GetAttribute("xy")),
                size = DecodeVector2(e.GetAttribute("size")),
                type = e.GetAttribute("type"),
                blend = e.GetAttribute("blend"),
                controller = e.GetAttribute("controller"),
                fileName = e.GetAttribute("fileName"),
                src = e.GetAttribute("src"),
                pkg = e.GetAttribute("pkg"),
                group = e.GetAttribute("group"),
            };
            return c;
        }

        private UIController LoadController(XmlElement e, UIPackageItem item)
        {
            try
            {
                var c = new UIController(item, e)
                {
                    name = e.GetAttribute("name"),
                    homePage = e.GetAttribute("homePage"),
                    homePageType = e.GetAttribute("homePageType"),
                    selected = e.GetAttribute("selected"),
                };
                var pages = e.GetAttribute("pages");
                var kvt = pages.Split(',');
                if (kvt.Length > 1)
                {
                    for (int p = 0; p < kvt.Length; p += 2)
                    {
                        var page = new UIControllerPage(c, c.pages.Count);
                        page.name = kvt[p + 1];
                        c.pages.Add(page);
                    }
                }
                e.ForEachChilds((XmlElement remark) =>
                {
                    var page = remark.GetAttributeAs<int>("page");
                    var value = remark.GetAttributeAs<string>("value");
                    c.pages[page].remark = value;
                });
                return c;
            }
            catch
            {
                throw;
            }
        }
        private void LoadTreeView(UIPackageItem item)
        {
            foreach (var sub in item.treeView)
            {
                LoadTreeView(sub);
            }
            if (string.IsNullOrEmpty(item.fileName)) return;
            if (string.IsNullOrEmpty(item.src)) return;
            var pkg = item._res?._pkg;
            if (!string.IsNullOrEmpty(item.pkg) && TryGetPackage(item.pkg, out var _pkg))
            {
                pkg = _pkg;
            }
            if (pkg.resources.TryGetValue(item.src, out var reference))
            {
                item._src = reference;
                if (reference.component != null)
                {
                    item.treeView.AddRange(reference.component.treeView);
                }
            }
        }


        public static Vector2 DecodeVector2(string xy)
        {
            if (string.IsNullOrEmpty(xy))
            {
                return Vector2.Zero;
            }
            try
            {
                var kv = xy.Split(',');
                return new Vector2() { X = float.Parse(kv[0]), Y = float.Parse(kv[1]), };
            }
            catch
            {
                return new Vector2(0, 0);
            }
        }
    }
}
