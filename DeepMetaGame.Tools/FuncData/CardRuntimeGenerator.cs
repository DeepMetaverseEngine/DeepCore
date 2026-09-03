using DeepCore;
using DeepCore.Concurrent;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection.Modeling;
using DeepCore.Xml;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Tools.SaveAll;
using DeepTools.CodeGen;
using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp;
using System.Diagnostics;
using System.Text;
using System.Xml;
using static DeepCore.GUI.Cell.SpriteSet;
using static DeepMetaGame.Data.Template.CardTemplate;

namespace DeepMetaGame.Tools.FuncData
{
    public class CardRuntimeGenerator : CardRuntimeAdapter
    {
        private readonly EditorTemplatesData templates;
        private readonly SaveEditorRuntime runtime;
        public override Logger Log { get; } = new LazyLogger("CardRuntimeGenerator");
        public override IReadOnlyCollection<CardTemplate> AllOriginCards => templates.Cards.Values;
        public FileInfo CSProjFile => runtime.BakeCardCSProjFile;
        public CardRuntimeGenerator(EditorTemplatesData templates, SaveEditorRuntime runtime)
        {
            this.templates = templates;
            this.runtime = runtime;
        }
        public override bool TryGetOriginCard(int tableName, out CardTemplate card)
        {
            card = templates.Cards.Get(tableName);
            return card != null;
        }
        public override bool TryGetOriginTemplate(Type templateType, int templateID, out TemplateData temp)
        {
            return templates.TryGetTemplateData(templateType, templateID, out temp);
        }
        public override IReadOnlyCollection<TemplateData> GetAllTemplatesData()
        {
            return templates.AllTemplates();
        }

        public void Run(IRangeValue progress = null)
        {
            var output = runtime.BakeCardDir;
            var cards = new List<CardTemplate>(templates.Cards.Values);
            cards.Sort((a, b) => a.ID.CompareTo(b.ID));
            foreach (var card in cards)
            {
                progress?.SetText($"Bake Card Code : {card}");
                var temp = XmlUtil.LoadXML(Resource.LoadFromAssembly(typeof(CardRuntimeGenerator), $"card_affect.xml"));
                var affect = GenAffectBindings(card);
                GenCode(card, affect, temp);
                CFiles.WriteAllText(new FileInfo(output.FullName + Path.DirectorySeparatorChar + $"card_{card.ID}.cs"), temp.InnerText, CUtils.UTF8_BOM);
                progress?.Add(1);
            }
            {
                var doc = XmlUtil.LoadXML(Resource.LoadFromAssembly(typeof(CardRuntimeGenerator), $"card_affects.xml"));
                var temp = doc.DocumentElement;
                temp.ForEachChilds<XmlElement>("CARDS", t_cards =>
                {
                    var sb = new StringBuilder();
                    foreach (var card in cards)
                    {
                        var t_card = t_cards.Clone() as XmlElement;
                        t_card.SetChildInnerText("CARD_ID", c => $"{card.ID}");
                        t_card.SetChildInnerText("CARD_NAME", c => $"{card.Name}");
                        sb.Append(t_card.InnerText);
                    }
                    t_cards.InnerText = sb.ToString();
                });
                CFiles.WriteAllText(new FileInfo(output.FullName + Path.DirectorySeparatorChar + $"cards.cs"), temp.InnerText, CUtils.UTF8_BOM);
            }
            {
                var projText = CSPROJ_TEMPLATE.ReplaceAll("{HostDLL}", runtime.InputHostAssemblyName);
                CFiles.WriteAllBytes(CSProjFile, Encoding.UTF8.GetBytes(projText));
            }
            this.BuildCardProject();
        }

        private void GenCode(CardTemplate card, AffectBindingTemplates affect, XmlDocument doc)
        {
            var temp = doc.DocumentElement;
            temp.SetChildInnerText("CARD_ID", c => $"{card.ID}");
            temp.SetChildInnerText("CARD_NAME", c => $"{card.Name}");
            temp.ForEachChilds<XmlElement>("AFFECTS", t_affects =>
            {
                var sb = new StringBuilder();
                foreach (var affect in affect.Affects)
                {
                    foreach (var temps in affect.Value.Templates)
                    {
                        if (this.templates.TryGetTemplateData(affect.Key, temps.Key, out var tempData))
                        {
                            var t_affect = t_affects.Clone() as XmlElement;
                            t_affect.SetChildInnerText("TEMP_TYPE", (c) => $"{tempData.GetType().ToFuncVisibleName()}");
                            t_affect.SetChildInnerText("TEMP_TYPE_NAME", (c) => $"{tempData.GetType().Name}");
                            t_affect.SetChildInnerText("TEMP_ID", (c) => $"{tempData.ID}");
                            t_affect.SetChildInnerText("TEMP_NAME", c => $"{tempData.Name}");
                            t_affect.ForEachChilds<XmlElement>("FIELDS", t_fields =>
                            {
                                var sb2 = new StringBuilder();
                                var t_field = t_fields.Clone() as XmlElement;
                                var f_index = 0;
                                foreach (var field in temps.Value.FieldsUsage)
                                {
                                    t_field.SetChildInnerText("FIELD_DESC", (c) => $"{field.UmlNode.FieldDesc?.Desc}");
                                    t_field.SetChildInnerText("FIELD_UML_PATH", (c) => $"{GetTempUmlPath(tempData, field)}");
                                    t_field.SetChildInnerText("FIELD_OP", (c) => $" {field.Field.FieldOP.ToShortString()} ");
                                    t_field.SetChildInnerText("FIELD_VALUE", (c) => GetLevelSwitch(field, f_index));
                                    sb2.Append(t_field.InnerText);
                                    f_index++;
                                }
                                t_fields.InnerText = sb2.ToString();
                            });
                            sb.Append(t_affect.InnerText);
                        }
                    }
                }
                t_affects.InnerText = sb.ToString();
            }, true);
        }

        private static string GetLevelSwitch(AffectFieldUsage affect, int index)
        {
            var field = affect.Field;
            var ftype = field.FieldType.ToFuncVisibleName();
            // level switch {  0 =>1, _ =>0  } ;
            var sb = new StringBuilder();
            //if (affect.UmlNode.DeclearValueType != affect.Field.FieldType)
            {
                sb.Append($"({ftype}) ");
            }
            sb.Append("(level switch {");
            for (int lv = 0; lv < field.LevelsCount; lv++) { sb.Append($"{lv} => {GetLevelText(affect, field.Levels[lv])}, "); }
            sb.Append($"_ => default").Append("});");
            return sb.ToString();
        }
        private static string GetLevelText(AffectFieldUsage affect, CardFieldCell cell)
        {
            var field = affect.Field;
            var ftype = field.FieldType;
            if (ftype == typeof(string))
            {
                return $"\"{cell}\"";
            }
            if (ftype == typeof(float))
            {
                return $"{cell}f";
            }
            if (ftype == typeof(bool))
            {
                return $"{cell}".ToLower();
            }
            if (ftype.IsEnum)
            {
                return $"{ftype.ToFuncVisibleName()}.{cell}";
            }
            return cell.ToString();
        }
        public static string GetTempUmlPath(TemplateData tempData, AffectFieldUsage usage)
        {
            UmlNode uml = usage.UmlNode;
            var sb = new StringBuilder();
            while (uml != null)
            {
                if (uml is UmlValueNode value_uml)
                {
                    if (value_uml.ValueType == tempData.GetType())
                    {
                        break;
                    }
                    var text = $".{uml.Name}";
                    if (value_uml.ParentNode is UmlArrayNode)
                    {
                        text = $"[{value_uml.OwnerIndexer}]";
                    }
                    else if (value_uml.ParentNode is UmlMapNode)
                    {
                        text = $"[{value_uml.OwnerIndexer}]";
                    }
                    else if (value_uml.ParentNode is UmlListNode)
                    {
                        text = $"[{value_uml.OwnerIndexer}]";
                    }
                    if (!value_uml.ValueType.IsPrimitiveFuncType())
                    {
                        if (value_uml.DeclearValueType != value_uml.Value.GetType())
                        {
                            text += $".AS<{value_uml.Value.GetType().ToFuncVisibleName()}>()";
                        }
                    }
                    sb.Insert(0, text);
                }
                else
                {
                    sb.Insert(0, $".{uml.Name}");
                }
                uml = uml.ParentNode;
            }
            return sb.ToString();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------

        //@"
        // <Project Sdk=""Microsoft.NET.Sdk"">
        // 	<PropertyGroup>
        // 		<TargetFramework>netstandard2.1</TargetFramework>
        // 		<ImplicitUsings>enable</ImplicitUsings>
        // 		<Deterministic>true</Deterministic>
        // 		<LangVersion>latest</LangVersion>
        // 		<Configurations>Debug;Release;Source</Configurations>
        // 	</PropertyGroup>
        // 
        // 	<ItemGroup>
        // 		<Reference Include=""DeepCore"">
        // 			<HintPath>..\bin\DeepCore.dll</HintPath>
        // 		</Reference>	
        // 		<Reference Include=""DeepCore.Event"">
        // 			<HintPath>..\bin\DeepCore.Event.dll</HintPath>
        // 		</Reference>
        // 		<Reference Include=""DeepMetaGame.Host"">
        // 			<HintPath>..\bin\DeepMetaGame.Host.dll</HintPath>
        // 		</Reference>
        // 		<Reference Include=""DeepMetaGame.Data"">
        // 			<HintPath>..\bin\DeepMetaGame.Data.dll</HintPath>
        // 		</Reference>
        // 		<Reference Include=""{HostDLL}"">
        // 			<HintPath>..\bin\{HostDLL}.dll</HintPath>
        // 		</Reference>
        // 	</ItemGroup>
        // </Project>
        // ";
        public static string CSPROJ_TEMPLATE { get; set; } = Resource.LoadTextFromAssembly(typeof(CardRuntimeGenerator), $"card_csproj.xml");

        /*         
echo ===========================================
echo ==== Build GameEditor.Code ==== 
echo ===========================================
cd %~dp0\..\GameEditor\code\
dotnet build GameEditor.Code.csproj -o %~dp0\output --configuration Debug 

SET TargetDir=%~dp0\output

SET DST_ASSETS=%~dp0..\..\ProjectZuma\Assets
SET DST_RUNTIME=%~dp0\..\GameEditor\UnityZuma\Zuma_Data\Managed

echo - AOT DLL TO: %DST_ASSETS%\Plugins\Library\
copy /Y   %TargetDir%\GameEditor.Code.dll    %DST_ASSETS%\Plugins\Library\
copy /Y   %TargetDir%\GameEditor.Code.pdb    %DST_ASSETS%\Plugins\Library\

echo - AOT DLL TO: %DST_RUNTIME%\
copy /Y   %TargetDir%\GameEditor.Code.dll    %DST_RUNTIME%
copy /Y   %TargetDir%\GameEditor.Code.dll    %~dp0\..\GameEditor\bin\

IF NOT %ERRORLEVEL%==0 @pause

echo ===========================================
pause
         */
        public virtual int BuildCardProject()
        {
            var csproj = CSProjFile;
            var configuration = "Debug";
            var redis_start = new ProcessStartInfo();
            redis_start.WorkingDirectory = Path.GetDirectoryName(csproj.Directory.FullName);
            redis_start.FileName = "dotnet";
            redis_start.Arguments = $"build {CSProjFile.FullName} -o {csproj.Directory.FullName}/bin/{configuration} --configuration {configuration}";
            redis_start.UseShellExecute = false;
            Log.Info($"{redis_start.FileName} {redis_start.Arguments}");
            var p = Process.Start(redis_start);
            p.WaitForExit();
            Log.Info($"Done ExitCode={p.ExitCode}");
            return p.ExitCode;
        }
    }
}
