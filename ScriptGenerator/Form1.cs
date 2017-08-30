﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using ScintillaNET;
using ScintillaNET.Demo.Utils;
using ScriptGenerator.Properties;

namespace ScriptGenerator
{
    public partial class Form1 : Form
    {
        ScintillaNET.Scintilla TextArea;

        public Form1()
        {
            InitializeComponent();
        }

        private static DataTable GetDataTabletFromCSVFile(string path, bool isPreview = true)
        {
            DataTable csvData = new DataTable();

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(path))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    string[] colFields = csvReader.ReadFields();

                    foreach (string column in colFields)
                    {
                        DataColumn serialno = new DataColumn(column);
                        serialno.AllowDBNull = true;
                        csvData.Columns.Add(serialno);
                    }
                    var count = 0;
                    while (!csvReader.EndOfData)
                    {
                        if (isPreview)
                        {
                            if (count++ > 10)
                            {
                                return csvData;
                            }
                        }
                        string[] fieldData = csvReader.ReadFields();
                        DataRow dr = csvData.NewRow();
                        //Making empty value as empty
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == null)
                                fieldData[i] = string.Empty;

                            dr[i] = fieldData[i];
                        }
                        csvData.Rows.Add(dr);
                    }

                }
            }
            catch (Exception ex)
            {
            }
            return csvData;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "CSV Open File Dialog";
            fdlg.InitialDirectory = @"c:\";
            fdlg.Filter = "CSV Files (*.csv)|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            fdlg.InitialDirectory = Settings.Default.CSVFolder;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                string fileToOpen = fdlg.FileName;

                txtCSVFileInput.Text = fdlg.FileName;
                gvCSVPreview.DataSource = GetDataTabletFromCSVFile(fileToOpen);

                Settings.Default.CSVFilePath = fdlg.FileName;
                Settings.Default.CSVFolder = Path.GetDirectoryName(fdlg.FileName);
                Settings.Default.Save();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtCSVFileInput.Text))
            {
                MessageBox.Show("Please choose CSV File",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error// for Warning  
                    //MessageBoxIcon.Error // for Error 
                    //MessageBoxIcon.Information  // for Information
                    //MessageBoxIcon.Question // for Question
                );
                return;
            }
            if (TextArea.Text.Length == 0)
            {
                MessageBox.Show("SQL File required",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error// for Warning  
                    //MessageBoxIcon.Error // for Error 
                    //MessageBoxIcon.Information  // for Information
                    //MessageBoxIcon.Question // for Question
                );

                return;
            }
            button1.Text = "Generating";
            var data = GetDataTabletFromCSVFile(txtCSVFileInput.Text, false);

            var st = new StringBuilder();

            foreach (DataRow dataRow in data.Rows)
            {
                var dict = dataRow.Table.Columns
                    .Cast<DataColumn>()
                    .ToDictionary(c => c.ColumnName, c => dataRow[c]);


                var text = SmartFormat.Smart.Format(TextArea.Text, dict);

                st.AppendLine(text);
                st.AppendLine();
            }
            button1.Text = "Generate";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "SQL File|*.sql",
                Title = "Save an SQL File"
            };
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.  
            if (saveFileDialog1.FileName != "")
            {
                File.WriteAllText(saveFileDialog1.FileName, st.ToString());

                MessageBox.Show("File has been saved to " + saveFileDialog1.FileName);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "SQL Open File Dialog";
            fdlg.InitialDirectory = @"c:\";
            fdlg.Filter = "SQL Files (*.sql)|*.sql";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            fdlg.InitialDirectory = Settings.Default.SQLFolder;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadDataFromFile(fdlg.FileName);
                Settings.Default.SQLFilePath = fdlg.FileName;
                Settings.Default.SQLFolder = Path.GetDirectoryName(fdlg.FileName);
                Settings.Default.Save();
            }
        }

        private void LoadDataFromFile(string path)
        {
            if (File.Exists(path))
            {
                TextArea.Text = File.ReadAllText(path);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // CREATE CONTROL
            TextArea = new ScintillaNET.Scintilla();
            TextPanel.Controls.Add(TextArea);

            // BASIC CONFIG
            TextArea.Dock = System.Windows.Forms.DockStyle.Fill;
            TextArea.TextChanged += (this.OnTextChanged);

            // INITIAL VIEW CONFIG
            TextArea.WrapMode = WrapMode.None;
            TextArea.IndentationGuides = IndentView.LookBoth;

            // STYLING
            InitColors();
            InitSyntaxColoring();

            // NUMBER MARGIN
            InitNumberMargin();

            // BOOKMARK MARGIN
            InitBookmarkMargin();

            // CODE FOLDING MARGIN
            InitCodeFolding();

            // DRAG DROP
            InitDragDropFile();

            if (File.Exists(Settings.Default.SQLFilePath))
            {
                LoadDataFromFile(Settings.Default.SQLFilePath);
            }

            // INIT HOTKEYS
            InitHotkeys();

            if (Directory.Exists(Settings.Default.CSVFolder))
            {
                txtCSVFileInput.Text = Settings.Default.CSVFolder;
            }

            if (File.Exists(Settings.Default.CSVFilePath))
            {
                gvCSVPreview.DataSource = GetDataTabletFromCSVFile(Settings.Default.CSVFilePath);
            }
        }
        #region Numbers, Bookmarks, Code Folding

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0x2A211C;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0xB7B7B7;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        /// <summary>
        /// change this to whatever margin you want the bookmarks/breakpoints to show in
        /// </summary>
        private const int BOOKMARK_MARGIN = 2;
        private const int BOOKMARK_MARKER = 2;

        /// <summary>
        /// change this to whatever margin you want the code folding tree (+/-) to show in
        /// </summary>
        private const int FOLDING_MARGIN = 3;

        /// <summary>
        /// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
        /// </summary>
        private const bool CODEFOLDING_CIRCULAR = true;

        private void InitNumberMargin()
        {

            TextArea.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
            TextArea.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

            var nums = TextArea.Margins[NUMBER_MARGIN];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            TextArea.MarginClick += TextArea_MarginClick;
        }

        private void InitBookmarkMargin()
        {

            //TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

            var margin = TextArea.Margins[BOOKMARK_MARGIN];
            margin.Width = 20;
            margin.Sensitive = true;
            margin.Type = MarginType.Symbol;
            margin.Mask = (1 << BOOKMARK_MARKER);
            //margin.Cursor = MarginCursor.Arrow;

            var marker = TextArea.Markers[BOOKMARK_MARKER];
            marker.Symbol = MarkerSymbol.Circle;
            marker.SetBackColor(IntToColor(0xFF003B));
            marker.SetForeColor(IntToColor(0x000000));
            marker.SetAlpha(100);

        }

        private void InitCodeFolding()
        {

            TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));
            TextArea.SetFoldMarginHighlightColor(true, IntToColor(BACK_COLOR));

            // Enable code folding
            TextArea.SetProperty("fold", "1");
            TextArea.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            TextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
            TextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
            TextArea.Margins[FOLDING_MARGIN].Sensitive = true;
            TextArea.Margins[FOLDING_MARGIN].Width = 20;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                TextArea.Markers[i].SetForeColor(IntToColor(BACK_COLOR)); // styles for [+] and [-]
                TextArea.Markers[i].SetBackColor(IntToColor(FORE_COLOR)); // styles for [+] and [-]
            }

            // Configure folding markers with respective symbols
            TextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
            TextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
            TextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
            TextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            TextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
            TextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            TextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            TextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

        }

        private void TextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            if (e.Margin == BOOKMARK_MARGIN)
            {
                // Do we have a marker for this line?
                const uint mask = (1 << BOOKMARK_MARKER);
                var line = TextArea.Lines[TextArea.LineFromPosition(e.Position)];
                if ((line.MarkerGet() & mask) > 0)
                {
                    // Remove existing bookmark
                    line.MarkerDelete(BOOKMARK_MARKER);
                }
                else
                {
                    // Add bookmark
                    line.MarkerAdd(BOOKMARK_MARKER);
                }
            }
        }

        #endregion

        #region Drag & Drop File

        public void InitDragDropFile()
        {

            TextArea.AllowDrop = true;
            TextArea.DragEnter += delegate (object sender, DragEventArgs e) {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };
            TextArea.DragDrop += delegate (object sender, DragEventArgs e) {

                // get file drop
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {

                    Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
                    if (a != null)
                    {

                        string path = a.GetValue(0).ToString();

                        LoadDataFromFile(path);

                    }
                }
            };

        }

        #endregion
        private void OnTextChanged(object sender, EventArgs e)
        {
            
        }

        private void InitColors()
        {

            TextArea.SetSelectionBackColor(true, IntToColor(0x114D9C));

        }

        private void InitHotkeys()
        {

            // register the hotkeys with the form
            //HotKeyManager.AddHotKey(this, OpenSearch, Keys.F, true);
            //HotKeyManager.AddHotKey(this, OpenFindDialog, Keys.F, true, false, true);
            //HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.R, true);
            //HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.H, true);
            //HotKeyManager.AddHotKey(this, Uppercase, Keys.U, true);
            //HotKeyManager.AddHotKey(this, Lowercase, Keys.L, true);
            //HotKeyManager.AddHotKey(this, ZoomIn, Keys.Oemplus, true);
            //HotKeyManager.AddHotKey(this, ZoomOut, Keys.OemMinus, true);
            //HotKeyManager.AddHotKey(this, ZoomDefault, Keys.D0, true);
            //HotKeyManager.AddHotKey(this, CloseSearch, Keys.Escape);

            // remove conflicting hotkeys from scintilla
            TextArea.ClearCmdKey(Keys.Control | Keys.F);
            TextArea.ClearCmdKey(Keys.Control | Keys.R);
            TextArea.ClearCmdKey(Keys.Control | Keys.H);
            TextArea.ClearCmdKey(Keys.Control | Keys.L);
            TextArea.ClearCmdKey(Keys.Control | Keys.U);

        }

        private void InitSyntaxColoring()
        {
            // Configure the default style
            TextArea.StyleResetDefault();
            TextArea.Styles[Style.Default].Font = "Consolas";
            TextArea.Styles[Style.Default].Size = 10;
            TextArea.Styles[Style.Default].BackColor = IntToColor(0x212121);
            TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
            TextArea.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            //TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
            //TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
            //TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
            //TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
            //TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
            //TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
            //TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
            //TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
            //TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
            //TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
            //TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
            //TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
            //TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
            //TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
            //TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
            //TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);
            TextArea.Styles[Style.Sql.Word].ForeColor = Color.FromArgb(147, 199, 99);
            TextArea.Styles[Style.Sql.Word].Bold = true;
            TextArea.Styles[Style.Sql.Identifier].ForeColor = Color.FromArgb(255, 255, 255);
            TextArea.Styles[Style.Sql.Character].ForeColor = Color.FromArgb(236, 118, 0);
            TextArea.Styles[Style.Sql.Number].ForeColor = Color.FromArgb(255, 205, 34);
            TextArea.Styles[Style.Sql.Operator].ForeColor = Color.FromArgb(232, 226, 183);
            TextArea.Styles[Style.Sql.Comment].ForeColor = Color.FromArgb(102, 116, 123);
            TextArea.Styles[Style.Sql.CommentLine].ForeColor = Color.FromArgb(102, 116, 123);
            TextArea.Lexer = Lexer.Sql;

            TextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
            TextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");

          

        }

        #region Utils

        public static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }

        public void InvokeIfNeeded(Action action)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }
        }

        #endregion

        private void button4_Click(object sender, EventArgs e)
        {

            File.WriteAllText(Settings.Default.SQLFilePath, TextArea.Text);
            MessageBox.Show("File saved.");
        }
    }
}