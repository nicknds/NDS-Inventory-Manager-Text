using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class PanelMasterClass
        {
            #region Variables

            public static Program parent;

            public string presetPanelOptions;

            SortedList<string, PregeneratedPanels> generatedPanels = new SortedList<string, PregeneratedPanels>();

            double tempCapacity = 0;

            SortedList<string, int> assemblyList = new SortedList<string, int>(),
                                    disassemblyList = new SortedList<string, int>();

            Color defPanelForegroundColor = new Color(0.7019608f, 0.9294118f, 1f, 1f);

            BlockDefinition tempBlockOptionDefinition;
            PanelDefinition tempPanelDefinition;
            int tempProcessPanelOptionSurfaceIndex;
            List<PanelObject> tempPanelObjects;
            List<long> tempIndexList = NewLongList;
            bool tempSpan;

            FunctionIdentifier selfContainedIdentifier;
            public DateTime updateTime = Now;

            #endregion


            #region Short Links

            bool PauseTickRun => parent.PauseTickRun;
            bool IsStateRunning => parent.StateRunning(selfContainedIdentifier);
            bool RunStateManager => parent.StateManager(selfContainedIdentifier, true, true);
            public static List<PanelObject> NewPanelObjectList => new List<PanelObject>();
            SortedList<string, List<string>> settingsListsStrings => parent.settingsListsStrings;
            SortedList<string, List<long>> typedIndexes => parent.typedIndexes;
            Dictionary<long, BlockDefinition> managedBlocks => parent.managedBlocks;


            #endregion


            #region Methods

            void SetPanelDefinition(BlockDefinition managedBlock, int surfaceIndex)
            {
                managedBlock.panelDefinitionList[surfaceIndex] = new PanelDefinition { surfaceIndex = surfaceIndex, provider = !(managedBlock.block is IMyTextPanel), suffixes = settingsListsStrings[setKeyDefaultSuffixes], parent = managedBlock };
            }

            public static string PresetPanelOption(string itemCategoryString)
            {
                StringBuilder builder = NewBuilder;
                BuilderAppendLine(builder, $"Type=Item/Cargo/Output/Status/Span");
                AppendOption(builder, "Font=Monospace");
                AppendOption(builder, $"Categories={itemCategoryString}");
                AppendOption(builder, "Items=ingot:Iron|ore:Iron");
                AppendOption(builder, "Item Display=Standard|Detailed|CompactAmount|CompactPercent");
                AppendOption(builder, "Sorting=Alphabetical|AscendingAmount|DescendingAmount|AscendingPercent|DescendingPercent");
                AppendOption(builder, "Options=BelowQuota|HideProgressBar");
                AppendOption(builder, "Minimum Value=1");
                AppendOption(builder, "Maximum Value=150000");
                AppendOption(builder, "Number Suffixes=K|M|B|T");
                AppendOption(builder, "Text Color=0:0:0:255");
                AppendOption(builder, "Number Color=120:0:0:255");
                AppendOption(builder, "Back Color=255:255:255:0");
                AppendOption(builder, "Rows=15");
                AppendOption(builder, "Name Length=18");
                AppendOption(builder, "Decimals=2");
                AppendOption(builder, "Update Delay=1");
                AppendOption(builder, "Span ID=Span A");
                AppendOption(builder, "Span Child ID=Span B");
                return builder.ToString().Trim();
            }

            public void CheckPanel(BlockDefinition blockDefinition, int surfaceIndex = 0)
            {
                PanelDefinition panelDefinition = blockDefinition.panelDefinitionList[surfaceIndex];
                string hashKey = panelDefinition.EntityFlickerID();

                if (parent.antiflickerSet.Add(hashKey))
                {
                    IMyTextSurface surface = panelDefinition.Surface;
                    if (surface.ContentType != ContentType.TEXT_AND_IMAGE)
                        surface.ContentType = ContentType.TEXT_AND_IMAGE;

                    if (surface.Script != nothingType)
                        surface.Script = nothingType;

                    if (surface.ScriptForegroundColor == defPanelForegroundColor)
                    {
                        surface.ScriptForegroundColor = Color.Black;
                        surface.ScriptBackgroundColor = new Color(73, 141, 255, 255);
                    }
                }
            }

            void ClonePanelObjects(List<PanelObject> panelObjects, List<PanelObject> list)
            {
                panelObjects.Clear();
                panelObjects.AddRange(list.Select(p => p.Clone()));
            }

            string BlockStatusTitle(string title, int disabled)
            {
                string formTitle = title;
                if (disabled > 0)
                    formTitle += $" -({ShortNumber2(disabled, settingsListsStrings[setKeyDefaultSuffixes])})";
                return formTitle;
            }

            Vector2 NewVector2(float x = 0f, float y = 0f)
            {
                return new Vector2(x, y);
            }

            void AddOutputItem(PanelDefinition panelDefinition, string text)
            {
                panelDefinition.AddPanelDetail(text.PadRight(panelDefinition.nameLength));
            }

            void BlockStatus(long index, ref int assembling, ref int disassembling, ref int idle, ref int disabled, SortedList<string, int> assemblyList, SortedList<string, int> disassemblyList)
            {
                IMyTerminalBlock block = managedBlocks[index].block;
                MyInventoryItem item;
                MyProductionItem productionItem;
                string key;
                if (!parent.IsBlockOk(index) || !((IMyFunctionalBlock)block).Enabled)
                {
                    disabled++;
                    return;
                }
                if (block is IMyAssembler)
                {
                    IMyAssembler assembler = (IMyAssembler)block;
                    if (assembler.IsQueueEmpty)
                        idle++;
                    else
                    {
                        List<MyProductionItem> productionList = NewProductionList;
                        assembler.GetQueue(productionList);
                        productionItem = productionList[0];
                        key = BlueprintSubtype(productionItem);
                        if (parent.blueprintList.ContainsKey(key))
                        {
                            Blueprint blueprint = parent.blueprintList[key];
                            key = parent.ItemName(blueprint.typeID, blueprint.subtypeID);
                        }
                        if (assembler.Mode == assemblyMode)
                        {
                            assembling++;
                            if (!assemblyList.ContainsKey(key))
                                assemblyList[key] = (int)productionItem.Amount;
                            else
                                assemblyList[key] += (int)productionItem.Amount;
                        }
                        else
                        {
                            disassembling++;
                            if (!disassemblyList.ContainsKey(key))
                                disassemblyList[key] = (int)productionItem.Amount;
                            else
                                disassemblyList[key] += (int)productionItem.Amount;
                        }
                    }
                }
                else if (block is IMyRefinery)
                {
                    if (block.GetInventory(0).ItemCount == 0)
                        idle++;
                    else
                    {
                        assembling++;
                        item = (MyInventoryItem)block.GetInventory(0).GetItemAt(0);
                        key = parent.ItemName(item);
                        if (!assemblyList.ContainsKey(key))
                            assemblyList[key] = (int)(item).Amount;
                        else
                            assemblyList[key] += (int)(item).Amount;
                    }
                }
                else if (block is IMyGasGenerator)
                {
                    if (block.GetInventory(0).ItemCount > 0)
                    {
                        item = (MyInventoryItem)block.GetInventory(0).GetItemAt(0);
                        key = parent.ItemName(item);
                        if (!assemblyList.ContainsKey(key))
                            assemblyList[key] = (int)item.Amount;
                        else
                            assemblyList[key] += (int)item.Amount;
                        if (parent.IsGas(item))
                            assembling++;
                        else
                            idle++;
                    }
                    else
                        idle++;
                }
            }

            string ProgressBar(float percent)
            {
                return $" [{"".PadRight((int)(6f * percent), '|').PadRight(6 - (int)(6f * percent), '.')}]";
            }

            Vector2 GetTextSize(StringBuilder builder, string font, IMyTextSurface surface)
            {
                if (!BuilderHasLength(builder)) return NewVector2();
                return surface.MeasureStringInPixels(builder, font, 1f);
            }

            #endregion


            #region State Functions

            public bool ProcessPanelOptions(BlockDefinition managedBlock, int surfaceIndex = 0)
            {
                selfContainedIdentifier = FunctionIdentifier.Process_Panel_Options;

                if (!IsStateRunning)
                {
                    tempBlockOptionDefinition = managedBlock;
                    tempProcessPanelOptionSurfaceIndex = surfaceIndex;
                }

                return RunStateManager;
            }

            public IEnumerator<FunctionState> ProcessPanelOptionState()
            {
                PanelDefinition panelDefinition;
                string dataSource, key, data, blockDefinition;
                StringBuilder keyBuilder = NewBuilder;
                string[] dataLines, dataOptions;
                yield return stateContinue;

                while (true)
                {
                    if (!tempBlockOptionDefinition.panelDefinitionList.ContainsKey(tempProcessPanelOptionSurfaceIndex))
                        SetPanelDefinition(tempBlockOptionDefinition, tempProcessPanelOptionSurfaceIndex);
                    panelDefinition = tempBlockOptionDefinition.panelDefinitionList[tempProcessPanelOptionSurfaceIndex];
                    dataSource = panelDefinition.DataSource;
                    blockDefinition = BlockSubtype(tempBlockOptionDefinition.block);

                    if (!TextHasLength(dataSource))
                    {
                        if (TextHasLength(panelDefinition.settingBackup))
                            SetPanelDefinition(tempBlockOptionDefinition, tempProcessPanelOptionSurfaceIndex);
                        if (parent.GetKeyBool(setKeyAutoTagBlocks))
                        {
                            panelDefinition.DataSource = presetPanelOptions;
                            tempBlockOptionDefinition.block.CustomName = tempBlockOptionDefinition.block.CustomName.Replace(panelTag, panelTag.ToUpper());
                        }
                    }
                    else if (!StringsMatch(dataSource, panelDefinition.settingBackup))
                    {
                        panelDefinition.itemSearchString = "";
                        panelDefinition.items.Clear();
                        keyBuilder.Clear();
                        dataLines = SplitLines(dataSource);
                        bool dataBool, rowSet = false;
                        double dataDouble;
                        int startIndex;
                        OptionHeaderIndex(out startIndex, dataLines, optionBlockFilter);
                        for (int i = startIndex; i < dataLines.Length; i++)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (startIndex > 0 && StringsMatch(dataLines[i], optionBlockFilter)) break;
                            if (!dataLines[i].StartsWith("//") && SplitData(dataLines[i], out key, out data))
                            {
                                dataBool = StringsMatch(data, trueString);
                                double.TryParse(data, out dataDouble);
                                switch (key.ToLower())
                                {
                                    case "type":
                                        switch (data.ToLower())
                                        {
                                            case "item":
                                                panelDefinition.panelType = PanelType.Item;
                                                break;
                                            case "cargo":
                                                panelDefinition.panelType = PanelType.Cargo;
                                                break;
                                            case "output":
                                                panelDefinition.panelType = PanelType.Output;
                                                break;
                                            case "status":
                                                panelDefinition.panelType = PanelType.Status;
                                                break;
                                            case "span":
                                                panelDefinition.panelType = PanelType.Span;
                                                break;
                                        }
                                        keyBuilder.Append($"{panelDefinition.panelType}");
                                        break;
                                    case "categories":
                                        dataOptions = data.ToLower().Split('|');
                                        panelDefinition.itemCategories.Clear();
                                        for (int x = 0; x < dataOptions.Length; x++)
                                        {
                                            if (PauseTickRun) yield return stateActive;
                                            if (parent.IsCategory(dataOptions[x]))
                                            {
                                                panelDefinition.itemCategories.Add(dataOptions[x]);
                                                keyBuilder.Append(dataOptions[x]);
                                            }
                                        }
                                        break;
                                    case "items":
                                        panelDefinition.itemSearchString = $"{(TextHasLength(panelDefinition.itemSearchString) ? $"{panelDefinition.itemSearchString}|" : "")}{data}";
                                        break;
                                    case "sorting":
                                        switch (data.ToLower())
                                        {
                                            case "alphabetical":
                                                panelDefinition.panelItemSorting = PanelItemSorting.Alphabetical;
                                                break;
                                            case "ascendingamount":
                                                panelDefinition.panelItemSorting = PanelItemSorting.AscendingAmount;
                                                break;
                                            case "descendingamount":
                                                panelDefinition.panelItemSorting = PanelItemSorting.DescendingAmount;
                                                break;
                                            case "ascendingpercent":
                                                panelDefinition.panelItemSorting = PanelItemSorting.AscendingPercent;
                                                break;
                                            case "descendingpercent":
                                                panelDefinition.panelItemSorting = PanelItemSorting.DescendingPercent;
                                                break;
                                        }
                                        keyBuilder.Append(panelDefinition.panelItemSorting.ToString());
                                        break;
                                    case "rows":
                                        panelDefinition.rows = (int)dataDouble;
                                        rowSet = true;
                                        break;
                                    case "name length":
                                        panelDefinition.nameLength = (int)dataDouble;
                                        keyBuilder.Append(panelDefinition.nameLength);
                                        break;
                                    case "decimals":
                                        panelDefinition.decimals = (int)dataDouble;
                                        keyBuilder.Append(panelDefinition.decimals);
                                        break;
                                    case "update delay":
                                        panelDefinition.updateDelay = dataDouble;
                                        break;
                                    case "span id":
                                        panelDefinition.spanKey = data;
                                        break;
                                    case "span child id":
                                        panelDefinition.childSpanKey = data;
                                        panelDefinition.span = TextHasLength(data);
                                        break;
                                    case "number suffixes":
                                        dataOptions = data.Split('|');
                                        panelDefinition.suffixes.Clear();
                                        panelDefinition.suffixes.AddRange(dataOptions);
                                        break;
                                    case "options":
                                        dataOptions = data.ToLower().Split('|');
                                        panelDefinition.belowQuota = dataOptions.Contains("belowquota");
                                        panelDefinition.showProgressBar = !dataOptions.Contains("hideprogressbar");
                                        break;
                                    case "minimum value":
                                        panelDefinition.minimumItemAmount = dataDouble;
                                        keyBuilder.Append(panelDefinition.minimumItemAmount);
                                        break;
                                    case "maximum value":
                                        panelDefinition.maximumItemAmount = dataDouble;
                                        keyBuilder.Append(panelDefinition.maximumItemAmount);
                                        break;
                                    case "item display":
                                        switch (data.ToLower())
                                        {
                                            case "detailed":
                                                panelDefinition.displayType = DisplayType.Detailed;
                                                break;
                                            case "compactamount":
                                                panelDefinition.displayType = DisplayType.CompactAmount;
                                                break;
                                            case "standard":
                                                panelDefinition.displayType = DisplayType.Standard;
                                                break;
                                            case "compactpercent":
                                                panelDefinition.displayType = DisplayType.CompactPercent;
                                                break;
                                        }
                                        break;
                                }
                            }
                            keyBuilder.Append(panelDefinition.itemSearchString);
                        }
                        if (panelDefinition.panelType != PanelType.None)
                        {
                            IMyTextSurface surface = panelDefinition.Surface;
                            panelDefinition.size = surface.SurfaceSize;
                            panelDefinition.settingKey = keyBuilder.ToString();
                            if (!rowSet)
                                panelDefinition.rows = 15;
                            switch (panelDefinition.panelType)
                            {
                                case PanelType.Cargo:
                                    if (!rowSet)
                                        panelDefinition.rows = panelDefinition.itemCategories.Count;
                                    break;
                                case PanelType.Output:
                                    if (!rowSet)
                                        panelDefinition.rows = 13;
                                    break;
                                case PanelType.Status:
                                    if (!rowSet)
                                        panelDefinition.rows = 8;
                                    break;
                            }
                        }
                        else
                            tempBlockOptionDefinition.panelDefinitionList.Remove(tempProcessPanelOptionSurfaceIndex);
                    }
                    if (updateTime < parent.itemAddedOrChanged ||
                        (panelDefinition.items.ItemTypeCount == 0 && TextHasLength(panelDefinition.itemSearchString)))
                    {
                        panelDefinition.items.Clear();
                        while (!parent.GetTags(panelDefinition.items, panelDefinition.itemSearchString))
                            yield return stateActive;
                    }
                    if (panelDefinition.items.ItemTypeCount > 0)
                        keyBuilder.Append(panelDefinition.items.ToString());

                    panelDefinition.settingBackup = dataSource;

                    yield return stateContinue;
                }
            }

            bool PopulateSprites()
            {
                selfContainedIdentifier = FunctionIdentifier.Main_Sprites;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> PopulateSpriteState()
            {
                yield return stateContinue;

                while (true)
                {
                    tempPanelDefinition.textBuilder.Clear();
                    if (tempPanelDefinition.panelObjects.Count > 0)
                        while (!PopulateSpriteList(tempPanelDefinition.panelObjects, false)) yield return stateActive;
                    if (tempPanelDefinition.spannableObjects.Count > 0)
                        while (!PopulateSpriteList(tempPanelDefinition.spannableObjects, true)) yield return stateActive;

                    yield return stateContinue;
                }
            }

            bool PopulateSpriteList(List<PanelObject> panelObjects, bool span)
            {
                selfContainedIdentifier = FunctionIdentifier.Generating_Sprites;

                if (!IsStateRunning)
                {
                    tempPanelObjects = panelObjects;
                    tempSpan = span;
                }

                return RunStateManager;
            }

            public IEnumerator<FunctionState> PopulateSpriteListState()
            {
                List<PanelObject> leftoverObjects = NewPanelObjectList;
                IMyTextSurface surface;
                int numberPadding, currentLine, maxLines;
                float percent;
                yield return stateContinue;

                while (true)
                {
                    leftoverObjects.Clear();
                    surface = tempPanelDefinition.Surface;
                    numberPadding = tempPanelDefinition.decimals + 4;
                    if (tempPanelDefinition.decimals > 0)
                        numberPadding++;
                    currentLine = 1;
                    maxLines = tempPanelDefinition.rows < 0 ? tempPanelObjects.Count : tempPanelDefinition.rows;

                    //Cycle objects
                    foreach (PanelObject panelObject in tempPanelObjects)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (!tempSpan || currentLine <= maxLines) // If objects are fixed (not spannable) and the row is within bounds
                        {
                            //Cycle details
                            foreach (PanelDetail panelDetail in panelObject.panelDetails)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (panelObject.item) // Process item
                                {
                                    percent = (float)(panelDetail.itemAmount / panelDetail.itemQuota);
                                    if (panelDetail.itemQuota <= 0f)
                                        percent = 1f;
                                    switch (tempPanelDefinition.displayType)
                                    {
                                        case DisplayType.CompactAmount:
                                            tempPanelDefinition.textBuilder.Append(panelDetail.itemName.PadRight(tempPanelDefinition.nameLength));
                                            tempPanelDefinition.textBuilder.AppendLine(ShortNumber2(panelDetail.itemAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding));
                                            break;
                                        case DisplayType.CompactPercent:
                                            tempPanelDefinition.textBuilder.Append(panelDetail.itemName.PadRight(tempPanelDefinition.nameLength));
                                            tempPanelDefinition.textBuilder.AppendLine(tempPanelDefinition.showProgressBar ? $"[{"".PadRight((int)(6f * percent), '|').PadRight(6 - (int)(6f * percent), '.')}]" : "");
                                            break;
                                        case DisplayType.Detailed:
                                            tempPanelDefinition.textBuilder.Append(panelDetail.itemName.PadRight(tempPanelDefinition.nameLength));
                                            tempPanelDefinition.textBuilder.Append(ShortNumber2(panelDetail.itemAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding));
                                            tempPanelDefinition.textBuilder.Append($"/{ShortNumber2(panelDetail.itemQuota, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding, false)}");
                                            tempPanelDefinition.textBuilder.AppendLine(tempPanelDefinition.showProgressBar ? $" {ProgressBar(percent)}" : "");
                                            if (panelDetail.assemblyAmount > 0)
                                                tempPanelDefinition.textBuilder.Append($"Assembling: {ShortNumber2(panelDetail.assemblyAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}");
                                            if (panelDetail.disassemblyAmount > 0)
                                            {
                                                if (panelDetail.assemblyAmount > 0)
                                                    tempPanelDefinition.textBuilder.Append(", ");
                                                tempPanelDefinition.textBuilder.Append($"Disassembling: {ShortNumber2(panelDetail.disassemblyAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}");
                                            }
                                            tempPanelDefinition.textBuilder.AppendLine($"Rate: {ShortNumber2(panelDetail.amountDifference, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}");
                                            break;
                                        case DisplayType.Standard:
                                            tempPanelDefinition.textBuilder.Append(panelDetail.itemName.PadRight(tempPanelDefinition.nameLength));
                                            tempPanelDefinition.textBuilder.Append(ShortNumber2(panelDetail.itemAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding));
                                            tempPanelDefinition.textBuilder.AppendLine($"/{ShortNumber2(panelDetail.itemQuota, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding, false)}");
                                            break;
                                    }
                                }
                                else if (!TextHasLength(panelDetail.textureType))
                                    tempPanelDefinition.textBuilder.AppendLine(TextHasLength(panelDetail.text) ? panelDetail.text : ShortNumber2(panelDetail.value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding));
                            }
                            if (tempSpan)
                                currentLine++;
                        }
                        else if (tempPanelDefinition.span)
                            leftoverObjects.Add(panelObject.Clone());
                        else
                            break;
                    }

                    foreach (SpanKey spanKey in tempPanelDefinition.spannedPanelList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        managedBlocks[spanKey.index].panelDefinitionList[spanKey.surfaceIndex].spannableObjects.Clear();
                        if (leftoverObjects.Count > 0)
                            ClonePanelObjects(managedBlocks[spanKey.index].panelDefinitionList[spanKey.surfaceIndex].spannableObjects, leftoverObjects);
                    }

                    yield return stateContinue;
                }
            }

            public bool TotalPanelV2(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = FunctionIdentifier.Main_Panel;

                if (!IsStateRunning)
                    tempPanelDefinition = panelDefinition;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> TotalPanelStateV2()
            {
                yield return stateContinue;

                while (true)
                {
                    if (Now >= tempPanelDefinition.nextUpdateTime)
                    {
                        string panelOptionString = tempPanelDefinition.settingKey;
                        tempPanelDefinition.panelObjects.Clear();
                        if (tempPanelDefinition.panelType != PanelType.Span)
                        {
                            bool cachePanel = TextHasLength(panelOptionString);
                            tempPanelDefinition.spannableObjects.Clear();
                            if (cachePanel && generatedPanels.ContainsKey(panelOptionString) && generatedPanels[panelOptionString].nextUpdateTime > Now)
                            {
                                ClonePanelObjects(tempPanelDefinition.panelObjects, generatedPanels[panelOptionString].panelObjects);
                                ClonePanelObjects(tempPanelDefinition.spannableObjects, generatedPanels[panelOptionString].spannableObjects);
                            }
                            else
                            {
                                switch (tempPanelDefinition.panelType)
                                {
                                    case PanelType.Cargo:
                                        while (!CargoPanel()) yield return stateActive;
                                        break;
                                    case PanelType.Item:
                                        while (!ItemPanel()) yield return stateActive;
                                        break;
                                    case PanelType.Output:
                                        while (!OutputPanel()) yield return stateActive;
                                        break;
                                    case PanelType.Status:
                                        while (!StatusPanel()) yield return stateActive;
                                        break;
                                }
                                if (cachePanel)
                                {
                                    if (!generatedPanels.ContainsKey(panelOptionString))
                                        generatedPanels[panelOptionString] = new PregeneratedPanels();
                                    else
                                    {
                                        generatedPanels[panelOptionString].panelObjects.Clear();
                                        generatedPanels[panelOptionString].spannableObjects.Clear();
                                    }
                                    ClonePanelObjects(generatedPanels[panelOptionString].panelObjects, tempPanelDefinition.panelObjects);
                                    ClonePanelObjects(generatedPanels[panelOptionString].spannableObjects, tempPanelDefinition.spannableObjects);
                                    generatedPanels[panelOptionString].nextUpdateTime = Now.AddSeconds(tempPanelDefinition.updateDelay);
                                }
                            }
                        }
                        tempPanelDefinition.nextUpdateTime = Now.AddSeconds(tempPanelDefinition.updateDelay);

                        while (!PopulateSprites()) yield return stateActive;
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.Surface.WriteText(tempPanelDefinition.textBuilder);
                        if (tempPanelDefinition.Surface.FontSize == 1f && BuilderHasLength(tempPanelDefinition.textBuilder))
                        {
                            Vector2 fontSize = GetTextSize(tempPanelDefinition.textBuilder, tempPanelDefinition.Surface.Font, tempPanelDefinition.Surface);
                            tempPanelDefinition.Surface.FontSize = Math.Min(tempPanelDefinition.size.X / fontSize.X, tempPanelDefinition.size.Y / fontSize.Y) * ((100f - (tempPanelDefinition.Surface.TextPadding * 2f)) / 100F);
                        }
                        for (int i = 0; i < generatedPanels.Count; i += 0)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if ((Now - generatedPanels.Values[i].nextUpdateTime).TotalSeconds >= 60)
                                generatedPanels.RemoveAt(i);
                            else
                                i++;
                        }
                    }
                    yield return stateContinue;
                }
            }

            bool CargoPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Cargo_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> CargoPanelState()
            {
                double capacity;
                yield return stateContinue;

                while (true)
                {
                    capacity = 0;
                    if (tempPanelDefinition.itemCategories.Contains("all"))
                    {
                        while (!CargoCapacity(ref capacity, typedIndexes[setKeyIndexStorage])) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"{"Total:".PadRight(tempPanelDefinition.nameLength)}{(tempPanelDefinition.showProgressBar ? $"{ProgressBar((float)capacity)} " : "")}{ShortNumber2(capacity * 100.0, tempPanelDefinition.suffixes, 2, 5)}%", false);
                    }
                    foreach (string category in tempPanelDefinition.itemCategories)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (category != "all" && parent.indexesStorageLists.ContainsKey(category))
                        {
                            while (!CargoCapacity(ref capacity, parent.indexesStorageLists[category])) yield return stateActive;
                            tempPanelDefinition.AddPanelDetail($"{$"{Formatted(category)}:".PadRight(tempPanelDefinition.nameLength)}{(tempPanelDefinition.showProgressBar ? $"{ProgressBar((float)capacity)} " : "")}{ShortNumber2(capacity * 100.0, tempPanelDefinition.suffixes, 2, 5)}%", false);
                        }
                    }
                    yield return stateContinue;
                }
            }

            bool CargoCapacity(ref double percentage, List<long> indexList)
            {
                selfContainedIdentifier = FunctionIdentifier.Measuring_Capacities;

                if (!IsStateRunning)
                {
                    tempIndexList.Clear();
                    tempIndexList.AddRange(indexList);
                }

                if (RunStateManager)
                {
                    percentage = tempCapacity;
                    return true;
                }
                return false;
            }

            public IEnumerator<FunctionState> CargoCapacityState()
            {
                double max, current;
                IMyInventory inventory;
                yield return stateContinue;

                while (true)
                {
                    max = current = 0;

                    foreach (long index in tempIndexList)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (!parent.IsBlockOk(index))
                            continue;

                        inventory = managedBlocks[index].Input;
                        max += (double)inventory.MaxVolume;
                        current += (double)inventory.CurrentVolume;
                    }
                    tempCapacity = current / max;

                    yield return stateContinue;
                }
            }

            bool ItemPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Item_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> ItemPanelState()
            {
                List<ItemDefinition> allItemList = new List<ItemDefinition>(), foundItemList = new List<ItemDefinition>();
                yield return stateContinue;

                while (true)
                {
                    allItemList.Clear();
                    foundItemList.Clear();
                    allItemList.AddRange(parent.GetAllItems);
                    bool found;
                    foreach (ItemDefinition item in allItemList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        found = tempPanelDefinition.itemCategories.Contains(item.category);
                        if (!found) tempPanelDefinition.items.ItemCount(out found, item.typeID, item.subtypeID, null);
                        if (found && item.display && item.amount >= tempPanelDefinition.minimumItemAmount && item.amount <= tempPanelDefinition.maximumItemAmount &&
                            (!tempPanelDefinition.belowQuota || item.amount < item.currentQuota))
                            foundItemList.Add(item);
                    }
                    switch (tempPanelDefinition.panelItemSorting)
                    {
                        case PanelItemSorting.Alphabetical:
                            foundItemList = foundItemList.OrderBy(x => x.displayName).ToList();
                            break;
                        case PanelItemSorting.AscendingAmount:
                            foundItemList = foundItemList.OrderBy(x => x.amount).ToList();
                            break;
                        case PanelItemSorting.DescendingAmount:
                            foundItemList = foundItemList.OrderByDescending(x => x.amount).ToList();
                            break;
                        case PanelItemSorting.AscendingPercent:
                            foundItemList = foundItemList.OrderBy(x => x.Percentage).ToList();
                            break;
                        case PanelItemSorting.DescendingPercent:
                            foundItemList = foundItemList.OrderByDescending(x => x.Percentage).ToList();
                            break;
                    }
                    foreach (ItemDefinition item in foundItemList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelItem(item.displayName.PadRight(tempPanelDefinition.nameLength), item.amount, item.currentQuota, item.queuedAssemblyAmount, item.queuedDisassemblyAmount, item.amountDifference);
                    }
                    yield return stateContinue;
                }
            }

            bool OutputPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Output_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> OutputPanelState()
            {
                List<OutputObject> tempOutputList = new List<OutputObject>();
                yield return stateContinue;

                while (true)
                {
                    AddOutputItem(tempPanelDefinition, $"NDS Inventory Manager {scriptVersion}");
                    AddOutputItem(tempPanelDefinition, currentMajorFunction.Replace("_", " "));
                    AddOutputItem(tempPanelDefinition, $"Runtime:    {parent.ShortMSTime(torchAverage)}");
                    AddOutputItem(tempPanelDefinition, $"Blocks:     {ShortNumber2(managedBlocks.Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Storages:   {ShortNumber2(typedIndexes[setKeyIndexStorage].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Assemblers: {ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"H2/O2 Gens: {ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Refineries: {ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"H2 Tanks:   {ShortNumber2(typedIndexes[setKeyIndexHydrogenTank].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"O2 Tanks:   {ShortNumber2(typedIndexes[setKeyIndexOxygenTank].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Weapons:    {ShortNumber2(typedIndexes[setKeyIndexGun].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Reactors:   {ShortNumber2(typedIndexes[setKeyIndexReactor].Count, tempPanelDefinition.suffixes)}");

                    tempOutputList.Clear();
                    tempOutputList.AddRange(parent.errorFilter ? parent.outputErrorList : parent.outputList);

                    AddOutputItem(tempPanelDefinition, parent.errorFilter ? $"Errors:     {ShortNumber2(parent.currentErrorCount, tempPanelDefinition.suffixes, 0, 6)} of {ShortNumber2(parent.totalErrorCount, tempPanelDefinition.suffixes, 0, 6)}" : $"Status:  {ShortNumber2(parent.scriptHealth, null, 3, 6)}%");

                    foreach (OutputObject outputObject in tempOutputList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail(outputObject.Output, true);
                    }

                    yield return stateContinue;
                }
            }

            bool StatusPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Status_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> StatusPanelState()
            {
                int assembling, disassembling, idle, disabled;
                List<long> tempIndices = new List<long>();
                yield return stateContinue;

                while (true)
                {
                    assembling = disassembling = idle = disabled = 0;
                    assemblyList.Clear();
                    disassemblyList.Clear();
                    tempIndices.AddRange(typedIndexes[setKeyIndexAssemblers]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    tempIndices.Clear();
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Assembling x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }
                    foreach (KeyValuePair<string, int> kvp in disassemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Disassembling x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }
                    AddOutputItem(tempPanelDefinition, BlockStatusTitle($"Assemblers x{ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)}", disabled).PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Assembling:    {ShortNumber2(assembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Disassembling: {ShortNumber2(disassembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Idle:          {ShortNumber2(idle, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    assembling = idle = disabled = 0;
                    assemblyList.Clear();
                    tempIndices.AddRange(typedIndexes[setKeyIndexRefinery]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    tempIndices.Clear();
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Refining x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }
                    AddOutputItem(tempPanelDefinition, BlockStatusTitle($"Refineries x{ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)}", disabled).PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Refining:      {ShortNumber2(assembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Idle:          {ShortNumber2(idle, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    assembling = idle = disabled = 0;
                    assemblyList.Clear();
                    tempIndices.AddRange(typedIndexes[setKeyIndexGasGenerators]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    tempIndices.Clear();
                    AddOutputItem(tempPanelDefinition, BlockStatusTitle($"O2/H2 Gens x{ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)}", disabled).PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Active:        {ShortNumber2(assembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Idle:          {ShortNumber2(idle, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Processing x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }

                    yield return stateContinue;
                }
            }

            #endregion

            public class PregeneratedPanels
            {
                public DateTime nextUpdateTime = Now.AddSeconds(1);

                public List<PanelObject> panelObjects = NewPanelObjectList, spannableObjects = NewPanelObjectList;
            }

            public class PanelDetail
            {
                public string itemName = "", text = "", textureType = "";
                public double itemAmount = 0, itemQuota = 0, value = 0, assemblyAmount = 0, disassemblyAmount = 0, amountDifference = 0;

                public PanelDetail Clone()
                {
                    PanelDetail panelDetail = new PanelDetail()
                    {
                        itemName = itemName,
                        text = text,
                        textureType = textureType,
                        itemAmount = itemAmount,
                        itemQuota = itemQuota,
                        value = value,
                        assemblyAmount = assemblyAmount,
                        disassemblyAmount = disassemblyAmount,
                        amountDifference = amountDifference
                    };

                    return panelDetail;
                }
            }

            public class PanelObject
            {
                public double sortableValue = 0;
                public string backdropType = "SquareSimple", sortableText = "";
                public bool item = false;
                public List<PanelDetail> panelDetails = new List<PanelDetail>();

                public PanelObject Clone()
                {
                    PanelObject panelObject = new PanelObject
                    {
                        sortableValue = sortableValue,
                        backdropType = backdropType,
                        sortableText = sortableText,
                        item = item
                    };

                    foreach (PanelDetail panelDetail in panelDetails)
                        panelObject.panelDetails.Add(panelDetail.Clone());

                    return panelObject;
                }
            }

            public class PanelDefinition
            {
                public BlockDefinition parent;

                public List<PanelObject>
                    panelObjects = NewPanelObjectList,
                    spannableObjects = NewPanelObjectList;

                public List<string> itemCategories = NewStringList, suffixes;

                public List<SpanKey> spannedPanelList = new List<SpanKey>();

                public int decimals = 2, rows = -1, nameLength = 18, surfaceIndex = 0;

                public double updateDelay = 1, minimumItemAmount = 0, maximumItemAmount = double.MaxValue;

                public bool span = false, cornerPanel = false, belowQuota = false, showProgressBar = true, provider = false;

                public PanelItemSorting panelItemSorting = PanelItemSorting.Alphabetical;

                public PanelType panelType = PanelType.None;

                public DisplayType displayType = DisplayType.Standard;

                public StringBuilder textBuilder = new StringBuilder();

                public DateTime nextUpdateTime = Now;

                public ItemCollection items = NewCollection;

                public Color textColor = Color.Black, numberColor = Color.Black, backdropColor = Color.GhostWhite;

                public Vector2 size = new Vector2(1, 1);

                public string settingKey = "", spanKey = "", childSpanKey = "", settingBackup = "", itemSearchString = "";

                void AddPanelObject(bool spannable = false, bool item = false)
                {
                    if (spannable)
                        spannableObjects.Add(new PanelObject { item = item });
                    else
                        panelObjects.Add(new PanelObject());
                }

                public void AddPanelItem(string name, double amount, double quota, double assemblyAmount, double disassemblyAmount, double amountDifference)
                {
                    AddPanelObject(true, true);
                    spannableObjects[spannableObjects.Count - 1].sortableText = name.Trim();
                    if (panelItemSorting == PanelItemSorting.AscendingPercent || panelItemSorting == PanelItemSorting.DescendingPercent)
                        spannableObjects[spannableObjects.Count - 1].sortableValue = quota > 0 ? amount / quota : 0;
                    spannableObjects[spannableObjects.Count - 1].panelDetails.Add(new PanelDetail { itemAmount = amount, itemName = name, itemQuota = quota, assemblyAmount = assemblyAmount, disassemblyAmount = disassemblyAmount, amountDifference = amountDifference });
                }

                public void AddPanelDetail(string text, bool spannable = false, float ratio = 1f, bool nextObject = true, bool reservedArea = false)
                {
                    if (nextObject)
                        AddPanelObject(spannable);
                    if (spannable)
                        spannableObjects[spannableObjects.Count - 1].panelDetails.Add(new PanelDetail { text = text });
                    else
                        panelObjects[panelObjects.Count - 1].panelDetails.Add(new PanelDetail { text = text });
                }

                public IMyTextSurface Surface => provider ? ((IMyTextSurfaceProvider)parent.block).GetSurface(surfaceIndex) : (IMyTextPanel)parent.block;

                string DataKey => $"{panelTag}@{surfaceIndex}";

                public string DataSource
                {
                    get
                    {
                        if (provider)
                        {
                            int index;
                            string[] lines = SplitLines(parent.DataSource);
                            OptionHeaderIndex(out index, lines, DataKey);
                            if (index > 0)
                            {
                                StringBuilder builder = new StringBuilder();
                                for (int i = index; i < lines.Length; i++)
                                    if (StringsMatch(lines[i], DataKey))
                                        break;
                                    else
                                        builder.AppendLine(lines[i]);
                                return builder.ToString().TrimEnd();
                            }
                            return "";
                        }
                        return parent.DataSource;
                    }
                    set
                    {
                        StringBuilder builder = new StringBuilder();
                        if (provider && TextHasLength(parent.DataSource))
                        {
                            builder.AppendLine(parent.DataSource);
                            builder.AppendLine();
                            builder.AppendLine(DataKey);
                        }
                        builder.AppendLine(value);
                        if (provider)
                            builder.AppendLine(DataKey);
                        parent.DataSource = builder.ToString().TrimEnd();
                    }
                }

                public string EntityFlickerID()
                {
                    string id = parent.block.EntityId.ToString();
                    if (provider)
                        id += $":{surfaceIndex}";
                    return id;
                }
            }
        }
    }
}
