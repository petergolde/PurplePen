using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using System.Threading.Channels;

namespace PurplePen.ViewModels
{
    // ViewModel for the popup grid used in the DescriptionViewer
    // to show contextual actions like buttons and text inputs.
    public partial class DescriptionPopupViewModel: ViewModelBase
    {
        // Number of rows in the grid.
        [ObservableProperty] private int rows;

        // Number of columns in the grid.
        [ObservableProperty] private int columns;

        // Data about the description change that this popup is for.
        [ObservableProperty] private DescriptionChangeData descriptionChangeData;

        private SymbolDB symbolDB;
        private string langId;
        private Dictionary<string, string> customSymbolText;

        // The ID used for the "no symbol" item.
        public const string NoSymbolId = "none";

        private int currentRow, currentCol;

        // Items to display in the grid, each with row/column placement.
        public ObservableCollection<PopupGridItemViewModel> MenuItems { get; } = new();

        public DescriptionPopupViewModel(SymbolDB symbolDB, string langId, Dictionary<string, string> customSymbolText, int cellContentPixelSize, DescriptionChangeData descriptionChangeData, PopupConfigurationData popupConfigurationData)
        {
            this.symbolDB = symbolDB;
            this.langId = langId;
            this.customSymbolText = customSymbolText;
            this.DescriptionChangeData = descriptionChangeData;
            this.Columns = 8;

            this.MenuItems.Clear();
            SymbolImageCache.Instance.Configure(symbolDB, cellContentPixelSize); // Cache symbol images at the exact size that will be drawn, for best look.

            // Add all the items to the popup menu according to the configuration data,
            // which specifies what kinds of items to add.
            AddItems(popupConfigurationData);

            Rows = currentRow + (currentCol > 0 ? 1 : 0);
        }

        // Configure the popup menu with the give number of symbol columns (typically 1 or 8). 
        // It can optionally contrain symbols of one or two kinds (if two kinds, with separator).
        // It can optionally have a text box of a given width (1-8). If textBoxInfo is non-null, a textbox is requested
        // with textBoxInfo as the text.
        public void AddItems(PopupConfigurationData popupData)
        {
            if (popupData.KindFirst != (char)0) {
                AddSymbolsOfKind(popupData.KindFirst);
            }

            if (popupData.NoSymbol) {
                AddNoSymbol();
            }

            // Add symbols of the second kind, if any.
            if (popupData.KindSecond != (char)0) {
                // A second kind of symbol. First put a separator with the first one.
                AddSeparator();
                AddSymbolsOfKind(popupData.KindSecond);
            }

            // Add a text box, if requested.
            if (popupData.TextBoxInfo != null) {
                AddTextbox(popupData.TextBoxInfo, popupData.InitialText ?? "", popupData.TextBoxWidth);
            }
        }


        // Adds a full-width horizontal separator to the grid.
        private void AddSeparator()
        {
            SeparatorGridItemViewModel separator = new SeparatorGridItemViewModel();
            AddItem(separator, Columns);
        }

        // Add the "no item" button.
        private void AddNoSymbol()
        {
            IGraphicsBitmap image = SymbolImageCache.Instance.GetSymbolImage(NoSymbolId);
            ButtonGridItemViewModel button = new ButtonGridItemViewModel(image, null, MiscText.NoSymbol);
            AddItem(button, 1);
        }

        private void AddSymbolsOfKind(char kind)
        {
            foreach (Symbol symbol in symbolDB.AllSymbols) {
                if (symbol.Kind == kind && symbol.HasVisualImage) {
                    IGraphicsBitmap image = SymbolImageCache.Instance.GetSymbolImage(symbol.Id);
                    string text = symbol.GetName(langId);

                    // Handle custom symbol text.
                    if (customSymbolText.ContainsKey(symbol.Id)) {
                        string customText = customSymbolText[symbol.Id];
                        customText = customText.Replace("{0}", "").Trim();  // Remove {0} fillin.
                        text += string.Format(" ({0})", customText);         // add custom symbol text after the regular name for the symbol.
                    }

                    ButtonGridItemViewModel button = new ButtonGridItemViewModel(image, symbol, text);

                    AddItem(button, symbol.IsWide ? 8 : 1);
                }
            }
        }

        private void AddTextbox(string infoText, string initialText, int width)
        {
            TextBoxGridItemViewModel textBox = new TextBoxGridItemViewModel(initialText, infoText)
            {
                InputText = initialText
            };
            AddItem(textBox, width);
        }

        private void AddItem(PopupGridItemViewModel item, int columnSpan)
        {
            // Check if this item is too wide to fit on the current row
            if (currentCol + columnSpan > Columns) {
                // Drop to the next line
                currentRow++;
                currentCol = 0;
            }

            // Assign the calculated coordinates to the item
            item.Row = currentRow;
            item.Column = currentCol;
            item.ColumnSpan = columnSpan;

            MenuItems.Add(item);

            // Move the "cursor" forward by however many columns this item takes up
            currentCol += item.ColumnSpan;

            // If the cursor hits the exact edge, wrap to the next line
            if (currentCol >= Columns) {
                currentRow++;
                currentCol = 0;
            }
        }
    }

    // Represents one item in the popup grid with its position.
    public abstract partial class PopupGridItemViewModel : ObservableObject
    {
        // Grid row for this item.
        [ObservableProperty] private int row;

        // Grid column for this item.
        [ObservableProperty] private int column;

        // Number of columns this item spans (default 1).
        [ObservableProperty] private int columnSpan = 1;

        // Information text to put in the bottom description or a toolip.
        [ObservableProperty] private string infoText = "";
    }

    // A button item in the popup grid.
    public partial class ButtonGridItemViewModel : PopupGridItemViewModel
    {
        // Text displayed on the button.
        [ObservableProperty] private IGraphicsBitmap? buttonBitmap;

        [ObservableProperty] private Symbol? symbol;  // null used for "no symbol" button.

        public ButtonGridItemViewModel(IGraphicsBitmap bitmap, Symbol? symbol, string infoText)
        {
            ButtonBitmap = bitmap;
            Symbol = symbol;
            InfoText = infoText;
        }
    }

    // A horizontal separator item in the popup grid.
    public partial class SeparatorGridItemViewModel : PopupGridItemViewModel
    {
        public SeparatorGridItemViewModel()
        {
            InfoText = " ";
        }
    }

    // A text input item in the popup grid.
    public partial class TextBoxGridItemViewModel : PopupGridItemViewModel
    {
        // Current text value.
        [ObservableProperty] private string inputText = "";

        // Placeholder/watermark text.
        [ObservableProperty] private string placeholderText;

        public TextBoxGridItemViewModel()
        {
            PlaceholderText = "";
            InfoText = "";
        }

        public TextBoxGridItemViewModel(string placeholder, string infoText)
        {
            PlaceholderText = placeholder;
            InfoText = infoText;
        }
    }

    // Maintains a cache of symbol images for the description popup, keyed by symbol ID.
    class SymbolImageCache
    {
        public static SymbolImageCache Instance { get; } = new SymbolImageCache();

        private SymbolDB? symbolDB;
        private string standard = "";
        private int boxSize;

        private Dictionary<string, IGraphicsBitmap> cache = new();

        private SymbolImageCache() { }

        public void Configure(SymbolDB symbolDB, int boxSize)
        {
            if (this.symbolDB != symbolDB || this.boxSize != boxSize) {
                this.symbolDB = symbolDB;
                this.boxSize = boxSize;
                this.standard = symbolDB.Standard;
                ClearCache();
            }
        }

        public IGraphicsBitmap GetSymbolImage(string symbolID)
        {
            if (symbolDB == null)
                throw new InvalidOperationException("SymbolImageCache not configured with SymbolDB");

            if (symbolDB.Standard != standard) {
                // The symbolDB.Standard has changed. 
                ClearCache();
                standard = symbolDB.Standard;
            }

            if (! cache.ContainsKey(symbolID)) {
                if (symbolID == DescriptionPopupViewModel.NoSymbolId) {
                    // Draw the "no symbol" image, which is just a dark red box.
                    int pixelWidth = boxSize, pixelHeight = boxSize;
                    using (IBitmapGraphicsTarget grTarget = Services.BitmapGraphicsTargetProvider.CreateBitmapGraphicsTarget(pixelWidth, pixelHeight, CmykColor.FromCmyka(0, 0, 0, 0, 0), DefaultColorConverter.Instance)) {
                        grTarget.PushAntiAliasing(false);
                        RectangleF rect = new RectangleF(0, 0, pixelWidth, pixelHeight);
                        rect.Inflate(-1.5F, -1.5F);
                        object pen = new object();
                        grTarget.CreatePen(pen, CmykColor.FromColor(Color.DarkRed), 1, LineCapMode.Flat, LineJoinMode.Miter, 5);
                        grTarget.DrawRectangle(pen, rect);
                        cache[symbolID] = grTarget.FinishBitmap();
                    }
                }
                else {
                    Symbol symbol = symbolDB[symbolID];

                    int pixelWidth = symbol.IsWide ? boxSize * 8 : boxSize;
                    int pixelHeight = boxSize;

                    // Use transparent background.
                    using (IBitmapGraphicsTarget grTarget = Services.BitmapGraphicsTargetProvider.CreateBitmapGraphicsTarget(pixelWidth, pixelHeight, CmykColor.FromCmyka(0, 0, 0, 0, 0), DefaultColorConverter.Instance)) {
                        grTarget.PushAntiAliasing(true);
                        symbol.Draw(grTarget, CmykColor.FromColor(Color.Black), new RectangleF(0, 0, pixelWidth, pixelHeight));
                        cache[symbolID] = grTarget.FinishBitmap();
                    }
                }
            }

            return cache[symbolID];
        }

        private void ClearCache()
        {
            cache.Clear();
        }
    }

    public record class PopupConfigurationData(
        int Columns,
        char KindFirst,
        char KindSecond,
        bool NoSymbol,
        string? TextBoxInfo,
        string? InitialText,
        int TextBoxWidth
    );
}
