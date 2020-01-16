using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
//using System.Speech.Recognition;

namespace AutoKeys
{
    public partial class wndMain : Form
    {
        const int BWTHRESHOLD = 100;

        byte steShift = 0;
        byte steControl = 0;
        byte steE = 0;
        byte steMult = 0;
        const string saveFile = "save.asav";

        string codeTag = "";
        IntPtr prevWind;

        string recording;
        bool isRecording = false;
        long recordingStart;
        long lastMouseMove;

        bool listening = false;
        bool listeningRepeats = false;
        bool typing = false;
        bool stop = false;
        int speed = 100;
        object synLock = new object();
        int altT = 75;
        float zoom = 1f;
        DataGridViewCell eCell = null;
        string routineRepeats = "";
        int bkspForRepeats = 0;
        long lastMouseUpdate = 0;
        string MouseData = "";
        string lastMsg = "", lastRepeats = "";
        bool isRepeat = false;
        bool isSelfShortcut = false;
        int myEscapes = 0; // distinguish between mine and user's


        private enum tmrAsyncDataTypes
        {
            REMOVEFROMTITLE = 0,
            REMOVEEMPTYROW = 1,
            CURSORWAIT = 2,
            CURSORDEFAULT = 3
        }
        struct tmrAsyncData
        {
            public tmrAsyncData(tmrAsyncDataTypes action, object value)
            {
                this.action = action;
                this.value = value;
            }
            public tmrAsyncDataTypes action;
            public object value;
        }
        LinkedList<tmrAsyncData> tmrAsyncDataList = new LinkedList<tmrAsyncData>();

        Point lastMouseClick = new Point(0, 0);
        Boolean saved = true;
        //SoundPlayer snd = new SoundPlayer("cancel.wav");

        static KeysConverter kc = new KeysConverter();
        static ColorDialog cd = new ColorDialog();
        public static Timer tmrDelay = new Timer();

        LinkedList<DataGridViewRow> searchExcludedRows = new LinkedList<DataGridViewRow>();

        //SpeechRecognizer sr = new SpeechRecognizer();

        // recieve
        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        public wndMain()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        public bool IsActive(IntPtr handle)
        {
            IntPtr activeHandle = GetForegroundWindow();
            return (activeHandle == handle);
        }

        [DllImport("user32")]
        public static extern int SetCursorPos(int x, int y);

        private const int MOUSEEVENTF_MOVE = 0x0001; /* mouse move */
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002; /* left button down */
        private const int MOUSEEVENTF_LEFTUP = 0x0004; /* left button up */
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008; /* right button down */
        private const int MOUSEEVENTF_RIGHTUP = 0x0010; /* right button up */

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private void ini(object sender, EventArgs e)
        {
            wndMain_ResizeEnd(this, null);
            this.KeyPreview = true;

            tmrDelay.Tick += tmrDelayTick;
            tmrDelay.Interval = 750;

            string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var directory = System.IO.Path.GetDirectoryName(path);
            ColorConverter cc = new ColorConverter();
            dataView.Columns[dataView.ColumnCount - 1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataView.RowHeadersVisible = false;
            tmrAsync.Start();
            //convert();
            //return;

            try
            {
                StreamReader file = new StreamReader(saveFile);
                string entry;
                Color backCol;

                string first = file.ReadLine();
                if (first != null)
                {
                    dataView.Columns[0].Width = Convert.ToInt32(first);

                    // column widths
                    for (int i = 1; i < dataView.ColumnCount; i++)
                    {
                        dataView.Columns[i].Width = Convert.ToInt32(file.ReadLine());
                    }

                    // font, delays, and instant state
                    dataView.Font = new Font(dataView.Font.FontFamily, Convert.ToSingle(file.ReadLine()));
                    tmrDelay.Interval = Convert.ToInt32(file.ReadLine());
                    altT = Convert.ToInt32(file.ReadLine());
                    chkInstant.Checked = Convert.ToBoolean(file.ReadLine());
                    speed = Convert.ToInt32(file.ReadLine());
                    zoom = Convert.ToSingle(file.ReadLine());

                    // custom colours
                    int[] colors = new int[16];
                    for (int i = 0; i < 16; i++)
                    {
                        colors[i] = Convert.ToInt32(file.ReadLine());
                        //MessageBox.Show(cd.CustomColors[i].ToString());
                        //if (cd.CustomColors[i] != 1677215)
                    }
                    cd.CustomColors = colors;
                }

                int clms = dataView.ColumnCount;
                while ((entry = file.ReadLine()) != null)
                {
                    DataGridViewRow row = (DataGridViewRow)dataView.Rows[dataView.RowCount - 1].Clone();

                    entry = entry.Replace("@|nl", "\r\n");

                    int start = 0, length = entry.IndexOf(" @|1 ");
                    int tagLength;

                    row.Cells[0].Value = entry.Substring(0, length);
                    for (int i = 1; i <= dataView.ColumnCount; i++)
                    {

                        tagLength = (" @|" + i + " ").Length;
                        start += length + tagLength;
                        length = entry.IndexOf(" @|" + (i + 1) + " ") - start;

                        if (i == dataView.ColumnCount)
                        {
                            row.Tag = int.Parse(entry.Substring(start, length));
                            break;
                        }

                        row.Cells[i].Value = entry.Substring(start, length);
                        if (row.Cells[i].Value.ToString().StartsWith("#"))
                        {
                            backCol = (Color)cc.ConvertFromString((string)row.Cells[dataView.ColumnCount - 2].Value);
                            row.DefaultCellStyle.BackColor = backCol;
                            if (backCol.R + backCol.G + backCol.B < BWTHRESHOLD * 3)
                            {
                                row.DefaultCellStyle.ForeColor = Color.White;
                            }
                        }
                    }

                    //row.Cells[0].Value = entry.Substring(0, entry.IndexOf(" @|1 "));
                    //row.Cells[1].Value = entry.Substring(entry.IndexOf(" @|1 ") + 5, entry.IndexOf(" @|2 ") - entry.IndexOf(" @|1 ") - 5);
                    //row.Cells[3].Value = entry.Substring(entry.IndexOf(" @|3 ") + 5, entry.IndexOf(" @|4") - entry.IndexOf(" @|3 ") - 5);

                    //row.Cells[2].Value = entry.Substring(entry.IndexOf(" @|2 ") + 5, entry.IndexOf(" @|3 ") - entry.IndexOf(" @|2 ") - 5);
                    //if (row.Cells[2].Value.ToString().StartsWith("#"))
                    //{
                    //    backCol = (Color)cc.ConvertFromstring((string)row.Cells[2].Value);
                    //    row.DefaultCellStyle.BackColor = backCol;
                    //    if (backCol.R + backCol.G + backCol.B < BWTHRESHOLD * 3)
                    //    {
                    //        row.DefaultCellStyle.ForeColor = Color.White;
                    //    }
                    //}

                    dataView.Rows.Add(row);
                }

                file.Close();
            }
            catch (Exception x)
            {
                //MessageBox.Show(x.Message);
                try
                {
                    System.IO.File.WriteAllText(saveFile, "");
                    txtDelay_Leave(null, null);
                    txtFont_Leave(null, null);
                    txtAltTab_Leave(null, null);
                    txtSpeed_Leave(null, null);
                    txtZoom_Leave(null, null);
                    return;
                }
                catch (Exception ignored)
                {
                    MessageBox.Show(ignored.Message, "Autokeys");// "Fatal error");
                    Application.Exit();
                }
            }

            txtDelay_Leave(null, null);
            txtFont_Leave(null, null);
            txtAltTab_Leave(null, null);
            txtSpeed_Leave(null, null);
            txtZoom_Leave(null, null);
            saved = true;
            //Choices choices = new Choices();
            //choices.Add(new string[] { "copy", "paste" });
            //GrammarBuilder gb = new GrammarBuilder();
            //gb.Append(choices);
            //// Create the Grammar instance.
            //Grammar g = new Grammar(gb);
            //sr.LoadGrammar(g);
            //sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sr_SpeechRecognized);

            //wrkKeyChecker.RunWorkerAsync();
        }

        //void sr_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        //{
        //    return;
        //}

        //private void convert()
        //{
        //    try
        //    {
        //        System.IO.StreamReader file = new System.IO.StreamReader("old.txt");
        //        string all = file.ReadToEnd();

        //        int[] indexs = new int[4];
        //        indexs[3] = 0;

        //        int last = all.LastIndexOf(">>==");
        //        while(true)
        //        {
        //            indexs[0] = all.IndexOf("==<<", indexs[3]) + 4;
        //            indexs[1] = all.IndexOf(">>==", indexs[0]);
        //            indexs[2] = indexs[1] + 4;
        //            if (indexs[1] == last)
        //                break;
        //            indexs[3] = all.IndexOf("==<<", indexs[2]);

        //            DataGridViewRow row = (DataGridViewRow)dataView.Rows[dataView.RowCount - 1].Clone();
        //            row.Cells[0].Value = all.Substring(indexs[0], indexs[1] - indexs[0]).Replace("\n", "");
        //            row.Cells[3].Value = all.Substring(indexs[2], indexs[3] - indexs[2]).Replace("\n", "");

        //            dataView.Rows.Add(row);
        //        }
        //        file.Close();

        //    }
        //    catch (Exception x)
        //    {
        //        //MessageBox.Show(x.Message);
        //        System.IO.File.WriteAllText("save.txt", "");
        //    }
        //}

        private void wndMain_ResizeEnd(object sender, EventArgs e)
        {
            int y = txtSearch.Location.Y;
            txtSearch.Width = this.Width - 800;

            txtDelay.Location = new Point(this.Width - 69, y);
            lblDelay.Location = new Point(this.Width - 180, y);
            txtFont.Location = new Point(this.Width - 230, y);
            lblFont.Location = new Point(this.Width - 308, y);
            txtAltTab.Location = new Point(this.Width - 360, y);
            lblAltT.Location = new Point(this.Width - 445, y);
            txtSpeed.Location = new Point(this.Width - 495, y);
            lblSpeed.Location = new Point(this.Width - 555, y);
            txtZoom.Location = new Point(this.Width - 605, y);
            lblZoom.Location = new Point(this.Width - 660, y);

            //dataView_ColumnWidthChanged(this, null);
        }

        //handled by resize fill setting
        //private void dataView_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        //{
        //    lock (synLock)
        //    {
        //        saved = false;
        //    }

        //    int columns = dataView.ColumnCount;

        //    int restWidth = 36;
        //    for (int i = 0; i < columns - 1; i++)
        //    {
        //        restWidth += dataView.Columns[i].Width;
        //    }
        //    dataView.Columns[columns - 1].Width = this.Width - restWidth;
        //}

        private string toHex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private void dataView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.ColumnIndex < 0 || e.RowIndex < 0)
            {
                return;
            }

            switch (e.Button)
            {
                case MouseButtons.Right:

                    //MessageBox.Show(e.RowIndex.ToString() + ", " + dataView.RowCount);
                    DataGridViewCell cell = dataView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    if(e.ColumnIndex == dataView.Columns.Count - 2 && !dataView.Rows[cell.RowIndex].ReadOnly)
                    {
                        if(cd.ShowDialog() == DialogResult.OK)
                        {
                            bool addedNewRow = false;
                            foreach (DataGridViewCell c in dataView.SelectedCells)
                            {
                                if(c.RowIndex != dataView.RowCount - 1)
                                {
                                    setColors(dataView.Rows[c.RowIndex]);
                                }

                                if (!addedNewRow && c.RowIndex == dataView.RowCount - 1)    //if adding new
                                {
                                    addedNewRow = true;
                                    addNewRow(dataView.Columns.Count - 2);
                                    dataView.CurrentCell = dataView.Rows[dataView.RowCount - 2].Cells[dataView.ColumnCount - 1];
                                    dataView.BeginEdit(true);
                                }
                            }
                        }
                    }else if(e.ColumnIndex == 1)
                    {

                        if(dataView.SelectedCells.Count == 1)
                        {
                            dataView.CurrentCell = cell;
                            dataView.BeginEdit(true);
                        }
                        else
                        {
                            string input = Prompt.ShowDialog("Edit Group IDs", "AutoKeys");
                            if (!input.Equals(""))
                            {
                                bool addedNewRow = false;
                                foreach (DataGridViewCell c in dataView.SelectedCells)
                                {
                                    if (c.ColumnIndex == 1)
                                    {
                                        c.Value = input;
                                    }

                                    if (!addedNewRow && c.RowIndex == dataView.RowCount - 1)    //if adding new
                                    {
                                        addedNewRow = true;
                                        addNewRow(1);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        dataView.CurrentCell = cell;
                        //MessageBox.Show(dataView.CurrentCell.State.ToString());
                        dataView.BeginEdit(true);
                    }

                    saved = false;
                    break;
                case MouseButtons.Left:
                    if(steShift == 2 && steControl == 2)
                    {
                        lock (synLock)
                        {
                            this.listening = false;
                        }

                        dataView.ClearSelection();

                        string msg = dataView.Rows[e.RowIndex].Cells[3].Value.ToString();
                        msg = msg.Replace("@|!date", getDate());
                        Clipboard.SetText(msg);
                        wrkTyper = new BackgroundWorker();
                        wrkTyper.DoWork += new DoWorkEventHandler(this.wrkTyper_DoWork);
                        if(chkInstant.Checked)
                            Clipboard.SetText(msg.ToString());
                        wrkTyper.RunWorkerAsync(msg);
                    }
                    break;
            }

        }

        private void addNewRow(int col)
        { 
            dataView.Rows.AddCopy(dataView.RowCount-1);
            //DataGridViewRow temp = (DataGridViewRow)dataView.Rows[dataView.RowCount - 1].Clone();
            DataGridViewRow target = dataView.Rows[dataView.RowCount - 2];
            DataGridViewRow src = dataView.Rows[dataView.RowCount - 1];
            
            target.Cells[col].Value = src.Cells[col].Value;
            if(col == dataView.Columns.Count - 2)
                setColors(target);
            src.Cells[col].Value = null;

            for (int i = 0; i < dataView.SelectedCells.Count; i++)
            {
                DataGridViewCell c = dataView.SelectedCells[i];
                if (c.RowIndex == dataView.RowCount - 1)
                {
                    dataView.Rows[c.RowIndex - 1].Cells[c.ColumnIndex].Selected = true;
                    c.Selected = false;
                }
            }

            //MessageBox.Show(row.Cells.Count.ToString());
        }

        private void dataView_KeyDown(object sender, KeyEventArgs e)
        {
            //if(e.KeyCode == Keys.V && steControl == 2)
            //{
            //    //handled universally
            //}
            //else
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    LinkedList<int> indexs = new LinkedList<int>();
                    foreach (DataGridViewCell c in dataView.SelectedCells)
                    {
                        if (c.RowIndex == dataView.RowCount - 1)
                            continue;
                        if (dataView.Rows[c.RowIndex].DefaultCellStyle.BackColor == Color.LightGray && dataView.Rows[c.RowIndex].Cells[3].Value == null)
                            continue;
                        if (!indexs.Contains(c.RowIndex))
                            indexs.AddLast(c.RowIndex);
                    }

                    int offset;
                    LinkedList<int> used = new LinkedList<int>();
                    while (indexs.Count > 0)
                    {
                        offset = 0;
                        foreach (int i in used)
                        {
                            if (i < indexs.First.Value)
                                offset++;
                        }
                        dataView.Rows.RemoveAt(indexs.First.Value - offset);
                        used.AddLast(indexs.First.Value);
                        indexs.RemoveFirst();
                    }
                    break;
            }

        }

        private void dataView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            lock (synLock)
            {
                saved = false;
                eCell = null;
            }

            if (isRowEmpty(e.RowIndex, true))
                return;

            if (e.ColumnIndex == 0 && e.RowIndex < dataView.Rows.Count - 1)
            {
                if (dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null)
                {
                    return;
                }

                string newTag = dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().ToLower();
                dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = newTag;
                DataGridViewCell c;
                foreach (DataGridViewRow row in dataView.Rows)
                {
                    c = row.Cells[0];
                    if (c.Value == null)    // nothing in cell -> ok
                        continue;

                    if (c.RowIndex == e.RowIndex && c.ColumnIndex == e.ColumnIndex)     // checking its self -> ok
                        continue;

                    //if(c.RowIndex == dataView.Rows.Count - 2)
                    //{
                    //    DataGridViewRow rr = (DataGridViewRow)dataView.Rows[dataView.RowCount - 1].Clone();
                    //    dataView.Rows.Add(rr);
                    //}

                    if (c.Value.ToString().Equals(newTag))
                    {
                        MessageBox.Show("tag already exists!");
                        string autoTag = "";
                        int i = 2;
                        Boolean ok = true;
                        while (true)
                        {
                            autoTag = newTag + i.ToString();
                            DataGridViewCell tagCell;
                            ok = true;
                            foreach (DataGridViewRow r in dataView.Rows)
                            {
                                tagCell = r.Cells[0];
                                if (tagCell.Value == null)
                                    continue;

                                if (tagCell.Value.ToString().StartsWith(autoTag))
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            if (ok)
                            {
                                dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = autoTag;
                                return;
                            }
                            i++;
                        }
                    }
                }

                foreach (DataGridViewRow row in searchExcludedRows)
                {
                    c = row.Cells[0];
                    if (c.Value == null)    // nothing in cell -> ok
                        continue;

                    if (c.RowIndex == e.RowIndex && c.ColumnIndex == e.ColumnIndex)     // checking its self -> ok
                        continue;

                    //if(c.RowIndex == dataView.Rows.Count - 2)
                    //{
                    //    DataGridViewRow rr = (DataGridViewRow)dataView.Rows[dataView.RowCount - 1].Clone();
                    //    dataView.Rows.Add(rr);
                    //}

                    if (c.Value.ToString().Equals(newTag))
                    {
                        MessageBox.Show("tag already exists!");
                        string autoTag = "";
                        int i = 2;
                        Boolean ok = true;
                        while (true)
                        {
                            autoTag = newTag + i.ToString();
                            DataGridViewCell tagCell;
                            ok = true;
                            foreach (DataGridViewRow r in dataView.Rows)
                            {
                                tagCell = r.Cells[0];
                                if (tagCell.Value == null)
                                    continue;

                                if (tagCell.Value.ToString().StartsWith(autoTag))
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            if (ok)
                            {
                                dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = autoTag;
                                return;
                            }
                            i++;
                        }
                    }
                }

            }
        }

        //private void dataView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        //{
        //    lock (synLock)
        //    {
        //        saved = false;
        //        eCell = null;
        //    }

        //    if (e.RowIndex >= dataView.RowCount || e.RowIndex < 0)
        //        return;

        //    //MessageBox.Show(e.RowIndex.ToString());

        //    Boolean empty = true;
        //    foreach (DataGridViewCell c in dataView.Rows[e.RowIndex].Cells)
        //    {
        //        if (c.Value != null && !c.Value.ToString().Equals(""))
        //        {
        //            empty = false;
        //            break;
        //        }
        //    }

        //    int index = dataView.CurrentRow.Index;
        //    if (empty && e.RowIndex != dataView.Rows.Count - 1)
        //    {
        //        tmrDelRow.Tag = index;
        //        tmrDelRow.Start();
        //        return;
        //    }

        //    if (e.ColumnIndex == 0 && e.RowIndex < dataView.Rows.Count - 1)
        //    {
        //        if (dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null)
        //        {
        //            return;
        //        }

        //        string newTag = dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().ToLower();
        //        dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = newTag;
        //        DataGridViewCell c;
        //        foreach (DataGridViewRow row in dataView.Rows)
        //        {
        //            c = row.Cells[0];
        //            if (c.Value == null)    // nothing in cell -> ok
        //                continue;

        //            if (c.RowIndex == e.RowIndex && c.ColumnIndex == e.ColumnIndex)     // checking its self -> ok
        //                continue;

        //            //if(c.RowIndex == dataView.Rows.Count - 2)
        //            //{
        //            //    DataGridViewRow rr = (DataGridViewRow)dataView.Rows[dataView.RowCount - 1].Clone();
        //            //    dataView.Rows.Add(rr);
        //            //}

        //            if (c.Value.ToString().Equals(newTag))
        //            {
        //                MessageBox.Show("tag already exists!");
        //                string autoTag = "";
        //                int i = 2;
        //                Boolean ok = true;
        //                while (true)
        //                {
        //                    autoTag = newTag + i.ToString();
        //                    DataGridViewCell tagCell;
        //                    ok = true;
        //                    foreach (DataGridViewRow r in dataView.Rows)
        //                    {
        //                        tagCell = r.Cells[0];
        //                        if (tagCell.Value == null)
        //                            continue;

        //                        if (tagCell.Value.ToString().StartsWith(autoTag))
        //                        {
        //                            ok = false;
        //                            break;
        //                        }
        //                    }
        //                    if (ok)
        //                    {
        //                        dataView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = autoTag;
        //                        return;
        //                    }
        //                    i++;
        //                }
        //            }
        //        }
        //    }
        //}

        private void dataView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            lock(synLock)
            {
                eCell = dataView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
        }

        private bool isRowEmpty(int y, bool delete)
        {
            Boolean empty = true;
            foreach (DataGridViewCell c in dataView.Rows[y].Cells)
            {
                if (c.Value != null && !c.Value.ToString().Equals("") && c.ColumnIndex != dataView.ColumnCount - 2)
                {
                    empty = false;
                    break;
                }
            }

            int index = dataView.CurrentRow.Index;
            if (delete && empty && y != dataView.Rows.Count - 1)
            {
                tmrAsyncDataList.AddLast(new tmrAsyncData(tmrAsyncDataTypes.REMOVEEMPTYROW, y));
                return true;
            }

            return false;
        }

        private void setColors(DataGridViewRow r)
        {
            lock (synLock)
            {
                saved = false;
            }

            Color textCol = Color.Black;
            if (cd.Color.R + cd.Color.G + cd.Color.B < BWTHRESHOLD * 3)
            {
                textCol = Color.White;
            }

            if(!cd.Color.Equals(Color.White))
            {
                r.Cells[dataView.ColumnCount - 2].Value = toHex(cd.Color);
            }
            else
            {
                r.Cells[dataView.ColumnCount - 2].Value = null;
            }
            r.DefaultCellStyle.BackColor = cd.Color;
            r.DefaultCellStyle.ForeColor = textCol;

            //MessageBox.Show(toHex(cd.Color));
            //foreach (DataGridViewCell c in dataView.SelectedCells)
            //{
            //    //MessageBox.Show(dataView.SelectedCells.ToString());
            //    if (c.ColumnIndex == dataView.ColumnCount - 2)
            //    {
            //        c.Value = toHex(cd.Color);
            //        dataView.Rows[c.RowIndex].DefaultCellStyle.BackColor = cd.Color;
            //        dataView.Rows[c.RowIndex].DefaultCellStyle.ForeColor = textCol;
            //    }
            //}
        }

        private void dataView_MouseClick(object sender, MouseEventArgs e)
        {
            lastMouseClick.X = e.X;
            lastMouseClick.Y = e.Y;
        }

        //private void groupSort()
        //{
        //    LinkedList<string> myList = new LinkedList<string>();

        //    //get max string length per col
        //    int[] maxLength = new int[dataView.Columns.Count - 3];
        //    int curLength = 0;
        //    for (int i = 1; i < dataView.Columns.Count - 2; i++)
        //    {
        //        foreach (DataGridViewRow r in dataView.Rows)
        //        {
        //            if (r.Cells[i].Value == null)
        //                continue;

        //            curLength = r.Cells[i].Value.ToString().Length;
        //            if (maxLength[i-1] < curLength)
        //                maxLength[i-1] = curLength;
        //        }
        //    }

        //    //make string of each cell same length per column and add to mylist
        //    string curstring, rowstring = "";
        //    //int rowIndex = 0;
        //    foreach (DataGridViewRow r in dataView.Rows)
        //    {
        //        for (int i = 1; i < dataView.Columns.Count - 2; i++)
        //        {
        //            if (r.Cells[i].Value == null)
        //                curstring = "";
        //            else
        //                curstring = r.Cells[i].Value.ToString();

        //            while (curstring.Length < maxLength[i - 1])
        //                curstring += " ";

        //            rowstring += curstring;
        //        }
        //        myList.AddLast(new string((rowstring.ToLower() /*+ indexPack(rowIndex)*/).ToCharArray()));
        //        rowstring = "";
        //    }
        //    myList.RemoveLast();    // removes the last empty one;

        //    //quicksort using mylist
        //    LinkedListNode<string> piv = myList.Last, curr = myList.First, wall = myList.First, temp;
        //    int indexWall = 0, indexPiv = myList.Count - 1, indexCurr = 0;
        //    while (wall.Next != null)
        //    {
        //        while (curr != piv)
        //        {
        //            if (string.Compare(curr.Value, piv.Value) < 0)
        //            {
        //                if (curr.Equals(wall))
        //                    continue;

        //                temp = curr.Next;
        //                myList.Remove(curr);
        //                myList.AddBefore(wall, curr);

        //                dataView.Rows.InsertCopy(indexCurr, indexWall);
        //                dataView.Rows.RemoveAt(indexCurr);
        //                indexWall++;

        //                curr = temp;

        //            }
        //            else
        //            {
        //                curr = curr.Next;
        //            }

        //            indexCurr++;
        //        }

        //        myList.Remove(piv);
        //        myList.AddBefore(wall, piv);

        //        dataView.Rows.InsertCopy(indexPiv, indexWall);
        //        dataView.Rows.RemoveAt(indexPiv);
        //        indexWall++;

        //        piv = myList.Last;
        //        curr = wall;
        //    }
        //}

        private void dataView_Sorted(object sender, EventArgs e)
        {
            //Rectangle groupFirstCell = dataView.GetCellDisplayRectangle(1, 0, true);
            //Boolean grpSort = lastMouseClick.X >= groupFirstCell.X && lastMouseClick.X <= groupFirstCell.X + groupFirstCell.Width;

            //if (grpSort)
            //    groupSort();
        }

        //private void dataView_Sorted(object sender, EventArgs e)
        //{
        //    lock (synLock)
        //    {
        //        saved = false;
        //    }

        //    int index = 0;
        //    while (index < dataView.RowCount)
        //    {
        //        if (dataView.Rows[index].DefaultCellStyle.BackColor == Color.LightGray && dataView.Rows[index].Cells[3].Value == null)
        //        {
        //            dataView.Rows.RemoveAt(index);
        //            continue;
        //        }
        //        index++;
        //    }

        //    Rectangle groupFirstCell = dataView.GetCellDisplayRectangle(1, 0, true);
        //    Boolean grpSort = lastMouseClick.X >= groupFirstCell.X && lastMouseClick.X <= groupFirstCell.X + groupFirstCell.Width;

        //    Rectangle colorFirstCell = dataView.GetCellDisplayRectangle(2, 0, true);
        //    Boolean colSort = lastMouseClick.X >= colorFirstCell.X && lastMouseClick.X <= colorFirstCell.X + colorFirstCell.Width;

        //    if (grpSort || colSort)
        //    { 
        //        string group = "";
        //        LinkedList<int> indexs = new LinkedList<int>();

        //        int col = 1;
        //        if (colSort)
        //            col = 2;

        //        foreach (DataGridViewRow r in dataView.Rows)
        //        {
        //            if (r.Cells[col].Value == null)
        //            {
        //                group = "";
        //                continue;
        //            }

        //            if (!((string)r.Cells[col].Value).Equals(group))
        //            {
        //                indexs.AddLast(r.Index);
        //                group = r.Cells[col].Value.ToString();
        //            }
        //        }
        //        DataGridViewRow greyLine;
        //        int offset = 0;
        //        foreach (int i in indexs)
        //        {
        //            greyLine = new DataGridViewRow();
        //            greyLine = (DataGridViewRow)dataView.Rows[dataView.RowCount - 1].Clone();
        //            greyLine.ReadOnly = true;
        //            greyLine.DefaultCellStyle.BackColor = Color.LightGray;
        //            //MessageBox.Show(indexs.Count().ToString());
        //            dataView.Rows.Insert(i + offset, greyLine);
        //            offset++;
        //        }
        //    }
        //}

        //// The GetForegroundWindow function returns a handle to the foreground window
        //// (the window  with which the user is currently working).
        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //private static extern IntPtr GetForegroundWindow();

        // The GetWindowThreadProcessId function retrieves the identifier of the thread
        // that created the specified window and, optionally, the identifier of the
        // process that created the window.
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        //// Returns the name of the process owning the foreground window.
        //private string GetForegroundProcessName()
        //{
        //    IntPtr hwnd = GetForegroundWindow();

        //    // The foreground window can be NULL in certain circumstances, 
        //    // such as when a window is losing activation.
        //    if (hwnd == null)
        //        return "Unknown";

        //    uint pid;
        //    GetWindowThreadProcessId(hwnd, out pid);

        //    foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
        //    {
        //        if (p.Id == pid)
        //            return p.ProcessName;
        //    }

        //    return "Unknown";
        //}

        private string getDate()
        {
            string date = "";
            string[] month =
                {"January", "February", "March", "April",
                 "May", "June", "July", "August", "September",
                 "October", "November", "December"};
            DateTime today = System.DateTime.Today;
            date += month[today.Month - 1] + " ";
            date += today.Day + ", ";
            date += today.Year;

            return date;
        }

        //private string indexPack(int i)
        //{
        //    if (i < 10)
        //        return "000" + i.ToString();
        //    else if(i < 100)
        //        return "00" + i.ToString();
        //    else if(i < 1000)
        //        return "0" + i.ToString();
        //    else
        //        return i.ToString();
        //}

        //private int indexUnpack(LinkedListNode<string> s)
        //{
        //    string str = s.Value.ToString();
        //    str = str.Substring(str.Length - 5, 4);
        //    return int.Parse(str);
        //}


        //LinkedList<DataGridViewRow> myList = new LinkedList<DataGridViewRow>();
        //foreach(DataGridViewRow r in dataView.Rows)
        //{
        //    myList.AddLast(r);
        //}

        //myList.RemoveLast();

        //LinkedListNode<DataGridViewRow> start, end;
        //start = myList.First;
        //end = start;

        //for (int i = 1; i < dataView.ColumnCount - 2; i++)  // depth handled here, not self recursive
        //{
        //    while (!end.Equals(myList.Last))
        //    {
        //        getSection(start, end, i);

        //        if (start.Equals(end)) // single row in section -> nothing to sort
        //        {
        //            start = start.Next;
        //            continue;
        //        }

        //        sortSection(start, end, i);
        //        start = end.Next;
        //    }
        //}

        //private void getSection(LinkedListNode<DataGridViewRow> start, LinkedListNode<DataGridViewRow> end, int depth)
        //{

        //    LinkedListNode<DataGridViewRow> current = start;
        //    string sectionName = "";
        //    // get section
        //    sectionName = (string)current.Value.Cells[depth].Value;
        //    while (true)
        //    {
        //        if (current.Next == null)   //reached end
        //        {
        //            break;
        //        }

        //        if (!sectionName.Equals((string)current.Next.Value.Cells[depth].Value)) // reached new section
        //            break;

        //        current = current.Next;
        //    }
        //    end = current;
        //    //MessageBox.Show(start.Value.Index.ToString());
        //}

        //private void sortSection(LinkedListNode<DataGridViewRow> start, LinkedListNode<DataGridViewRow> end, int depth)
        //{
        //    LinkedListNode<DataGridViewRow> piv = end, curr = start, wall = start, temp;
        //    LinkedList<DataGridViewRow> list = start.List;
        //    Boolean done = false;
        //    while (true)
        //    {
        //        // if cur is < piv
        //        if(string.Compare(curr.Value.Cells[depth].Value.ToString(), piv.Value.Cells[depth].Value.ToString()) < 0)
        //        {
        //            temp = curr.Previous;
        //            list.Remove(curr);
        //            list.AddAfter(curr, wall.Previous);
        //            list.Remove(wall);
        //            list.AddAfter(wall, temp);

        //            temp = curr.Next;
        //            //MessageBox.Show(curr.Value.Index.ToString());
        //            curr = wall.Next;
        //            //MessageBox.Show(curr.Value.Index.ToString());
        //            wall = temp;
        //            //MessageBox.Show(curr.Value.Index.ToString());
        //        }
        //        else
        //        {
        //            curr = curr.Next;
        //            MessageBox.Show(start.Value.Index.ToString());
        //        }

        //        if (curr.Equals(end))
        //            done = true;
        //        else if (done)
        //            break;
        //    }
        //}

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape || steControl == 2)
            {
                return true;
            }else if(keyData == Keys.Enter)
            {
                if(txtSearch.Focused)
                {
                    lock (synLock)
                    {
                        saved = false;
                    }

                    Cursor.Current = Cursors.WaitCursor;

                    string search = txtSearch.Text.ToUpper();
                    LinkedList<int> indexs = new LinkedList<int>();
                    int i = -1;
                    foreach (DataGridViewRow r in dataView.Rows)
                    {
                        i++;
                        if (r.ReadOnly || r.IsNewRow)
                            continue;

                        if (r.Cells[0].Value != null && r.Cells[0].Value.ToString().ToUpper().Contains(search))
                            continue;
                        if (r.Cells[1].Value != null && r.Cells[1].Value.ToString().ToUpper().Contains(search))
                            continue;
                        if (r.Cells[3].Value != null && r.Cells[3].Value.ToString().ToUpper().Contains(search))
                            continue;

                        indexs.AddLast(i);
                        searchExcludedRows.AddLast(r);
                    }

                    int offset = 0;
                    while (indexs.Count() > 0)
                    {
                        dataView.Rows.RemoveAt(indexs.First.Value - offset);
                        indexs.RemoveFirst();
                        //System.Threading.Thread.Sleep(3);
                        offset++;
                    }

                    i = 0;
                    //indexs.Clear();
                    foreach (DataGridViewRow r in searchExcludedRows)
                    {
                        if (r.Cells[0].Value != null && r.Cells[0].Value.ToString().ToUpper().Contains(search) ||
                            r.Cells[1].Value != null && r.Cells[1].Value.ToString().ToUpper().Contains(search) ||
                            r.Cells[3].Value != null && r.Cells[3].Value.ToString().ToUpper().Contains(search))
                        {
                            dataView.Rows.Add(r);
                            //System.Threading.Thread.Sleep(3);
                            indexs.AddLast(i);
                        }
                        i++;
                    }

                    i = 0;
                    var node = searchExcludedRows.First;
                    while (indexs.Count() > 0)
                    {
                        var nextNode = node.Next;
                        if (i == indexs.First.Value)
                        {
                            searchExcludedRows.Remove(node);
                            indexs.RemoveFirst();
                        }
                        node = nextNode;
                        i++;
                    }

                    Cursor.Current = Cursors.Default;
                    //dataView_ColumnWidthChanged(null, null);
                    return true;
                }
                else if (GetForegroundWindow().Equals(this.Handle) && eCell == null) //im foreground -> enter was on me but NOT IN DATAVIEW
                {
                    dataView.Focus();
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void MouseIntercepted(IntPtr action, int x, int y)
        {
            //recording
            if(isRecording)
            {
                switch ((MouseMessages)action)
                {
                    case MouseMessages.WM_LBUTTONDOWN:
                        break;
                    case MouseMessages.WM_LBUTTONUP:
                        break;
                    case MouseMessages.WM_MOUSEMOVE:
                        break;
                    case MouseMessages.WM_MOUSEWHEEL:
                        break;
                    case MouseMessages.WM_RBUTTONDOWN:
                        break;
                    case MouseMessages.WM_RBUTTONUP:
                        break;
                }
                return;
            }

            string mouseBtn = "";
            if((MouseMessages)action == MouseMessages.WM_LBUTTONDOWN)
            {
                mouseBtn = "ML";
            }else
            {
                mouseBtn = "MR";
            }

            switch((MouseMessages) action)
            {
                 //"@|`" + mouseBtn + "(" + x.ToString() + ", " + y.ToString() + ")";
                case MouseMessages.WM_MOUSEMOVE:
                    const long delay = 200;
                    if(currentTimeMilis() - lastMouseUpdate > delay)
                    {
                        try
                        {
                            if (this.Text.Contains("("))
                            {
                                int start = this.Text.IndexOf("(");
                                int end = this.Text.IndexOf(")");
                                //MessageBox.Show(start.ToString() + " " +  end.ToString());
                                string replace = this.Text.Substring(start, end - start + 1);
                                //MessageBox.Show(replace);
                                this.Text = this.Text.Replace(replace, "");
                            }
                            this.Text += "(" + x.ToString() + ", " + y.ToString() + ")";
                            //string msg = "";
                            //msg += steControl.ToString() + ", " + steShift.ToString();
                            //msg += "\r\n" + ">" + codeTag + "<";
                            //MessageBox.Show(msg);
                        }
                        catch (Exception e)
                        {
                            //ignored
                        }
                        lastMouseUpdate = currentTimeMilis();
                    }
                    break;  
                case MouseMessages.WM_LBUTTONDOWN:
                case MouseMessages.WM_RBUTTONDOWN:
                    if (steControl == 2)
                    {
                        MouseData = "@|!" + mouseBtn + "(" + x.ToString() + ", " + y.ToString() + ")";
                        return;
                    }
                    break;
            }
            //lock(synLock)
            //{
            //    prevWind = GetForegroundWindow();
            //}
        }

        private long currentTimeMilis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public void KeydownIntercepted(Keys k)
        {

            //MessageBox.Show(k.ToString());
            lock (synLock)
            {
                // start/stop recording
                if (false)//steControl == 2 && steShift == 2 && k == Keys.Multiply)
                {
                    steMult = 2;
                    if (isRecording) //then stop
                    {
                        string path;
                        while (true)
                        {
                            this.Text = this.Text.Replace("[Recording]", "");
                            //enter name
                            path = "\\Recordings\\.autRec";
                            while (path.Equals("\\Recordings\\.autRec"))
                            {
                                path = "\\Recordings\\" + Prompt.ShowDialog("Enter a name for the recording", "AutoKeys") + ".autRec";
                            }

                            //check if name exists
                            if (Directory.Exists(path))
                            {
                                DialogResult dialogResult = MessageBox.Show("This name already exists! Would you like to overwrite?", "AutoKeys", MessageBoxButtons.YesNoCancel);
                                if (dialogResult == DialogResult.No)
                                {
                                    continue;
                                }
                                else if (dialogResult == DialogResult.Cancel)
                                {
                                    isRecording = !isRecording;
                                    return;
                                }
                            }

                            File.WriteAllText(path, recording);
                        }
                    }
                    else
                    {
                        this.Text += "[Recording]";
                        recording = ""; //reset the recording var
                    }

                    isRecording = !isRecording;
                }

                //records
                if (isRecording && steShift != 2 && steControl != 2 && steMult != 2)
                {
                    recording += k.ToString() + " [] " + currentTimeMilis().ToString() + "\r\n";
                    return;
                }

                // ESC IS FOR CANCEL -> must keep
                if (k == Keys.Escape)
                {
                    if(myEscapes > 0)
                    {
                        myEscapes--;
                    }
                    else
                    {
                        if (eCell != null && !typing)
                        {
                            object val = eCell.Value;
                            int x = eCell.ColumnIndex, y = eCell.RowIndex;
                            dataView.EndEdit();
                            dataView.Rows[y].Cells[x].Value = val;
                            return;
                        }
                        else
                        {
                            myEscapes = 0;
                            steShift = 0;
                            steControl = 0;
                            steE = 0;
                            this.listening = false;
                            this.Text = this.Text.Replace("[Listening]", "");
                            stop = true;
                            tmrDelay.Stop();
                            if (!this.Text.Contains("[X]"))
                                this.Text += "[X]";
                            //snd.play();
                            //System.Media.SystemSounds.Beep.Play();
                            routineRepeats = "";
                            return;
                        }
                    }
                }

                if (typing)
                {
                    return;
                }
                //MessageBox.Show(steControl.ToString());

                if (listeningRepeats)
                {
                    int n = 0;
                    String key = k.ToString();
                    if(key.Contains("D") && key.Length == 2)    //not from numpad, so must be erased
                    {
                        bkspForRepeats++;
                    }
                    key = key.Replace("D", "").Replace("NumPad", "");
                    if (k.Equals(Keys.LShiftKey))
                    {
                        return;
                    }else if (int.TryParse(key, out n))
                    {
                        routineRepeats += key;
                    }
                    //MessageBox.Show(k.ToString());
                    return;
                }

                switch (k)
                {
                    case Keys.LShiftKey:
                        steShift = 2;
                        stop = false;
                        if(!this.Text.Contains("[^]"))
                            this.Text += "[^]";
                        break;
                    case Keys.LControlKey:
                        //MessageBox.Show(steControl.ToString());
                        //MessageBox.Show(steShift.ToString());
                        steControl = 2;
                        stop = false;
                        if (!this.Text.Contains("[#]"))
                            this.Text += "[#]";
                        break;
                    case Keys.V:
                        if (steControl == 2 && dataView.Focused)
                        {
                            string newTxt = Clipboard.GetText();
                            if (newTxt == null)
                            {
                                return;
                            }

                            foreach (DataGridViewCell c in dataView.SelectedCells)
                            {
                                if (c.ColumnIndex == dataView.ColumnCount - 2)
                                {
                                    try
                                    {
                                        if (newTxt.Equals(""))
                                        {
                                            newTxt = "#FFFFFF";
                                        }
                                        ColorConverter cc = new ColorConverter();
                                        cd.Color = (Color)cc.ConvertFromString(newTxt);

                                        setColors(dataView.Rows[c.RowIndex]);
                                    }
                                    catch (Exception x)
                                    { }
                                }
                                else
                                {
                                    c.Value = newTxt;
                                }
                            }
                            return;
                        }
                        else if (steControl == 2 && eCell != null && GetForegroundWindow().Equals(this.Handle) && dataView.CurrentCell.EditType == typeof(DataGridViewTextBoxEditingControl))
                        {
                            //tmrDelay.Tag = Clipboard.GetText().Replace("@|!", "@|@|!L;@|!R;!");
                            //tmrDelayTick(null, null);
                            TextBox ed = ((TextBox)dataView.EditingControl);
                            int caret = ed.SelectionStart;
                            int selStart = ed.SelectionStart, selEnd = ed.SelectionStart + ed.SelectionLength;
                            ed.Text = ed.Text.Substring(0, selStart) + ed.Text.Substring(selEnd, ed.Text.Length - selEnd);  //remove selection
                            ed.Text = ed.Text.Insert(caret, Clipboard.GetText());
                            ed.Select(caret + Clipboard.GetText().Length, 0);
                            //MessageBox.Show(ed.SelectionStart.ToString());
                            return;
                        }
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    case Keys.C:
                        if (steControl == 2 && dataView.Focused)
                        {
                            DataGridViewCell c = dataView.SelectedCells[0];
                            if(c.Value != null)
                                Clipboard.SetText(c.Value.ToString());
                            return;
                        }else if(steControl == 2 && eCell != null && GetForegroundWindow().Equals(this.Handle) && dataView.CurrentCell.EditType == typeof(DataGridViewTextBoxEditingControl))
                        {
                            TextBox ed = ((TextBox)dataView.EditingControl);
                            Clipboard.SetText(ed.SelectedText);
                        }
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    case Keys.A:
                        if (steControl == 2 && eCell != null && dataView.CurrentCell.EditType == typeof(DataGridViewTextBoxEditingControl))
                        {
                            TextBox ed = ((TextBox)dataView.EditingControl);
                            ed.SelectAll();
                        }
                        break;
                    case Keys.S:
                        if (steControl == 2 && IsActive(this.Handle))
                        {
                            //snd.play();
                            save();
                            return;
                        }
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    case Keys.F:
                        //MessageBox.Show(k.ToString());
                        if (steControl == 2)
                        {
                            txtSearch.Focus();
                            return;
                        }
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    case Keys.D:
                        //MessageBox.Show(steControl.ToString());
                        if (steControl == 2 && eCell != null)
                        {
                            //MessageBox.Show((eCell == null).ToString());
                            //isSelfShortcut = true;
                            //tmrDelay.Tag = "@|`";
                            //tmrDelayTick(null, null);
                            TextBox ed = ((TextBox)dataView.EditingControl);
                            int caret = ed.SelectionStart;
                            int selStart = ed.SelectionStart, selEnd = ed.SelectionStart + ed.SelectionLength;
                            ed.Text = ed.Text.Substring(0, selStart) + ed.Text.Substring(selEnd, ed.Text.Length - selEnd);  //remove selection
                            ed.Text = ed.Text.Insert(caret, "@|!");
                            ed.Select(caret + 3, 0);
                            return;
                        }
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    case Keys.E:
                        if (steControl == 2 && steShift == 2 && !lastMsg.Equals(""))
                        {
                            tmrDelay.Tag = lastMsg;
                            routineRepeats = lastRepeats;
                            steE = 2;
                            isRepeat = true;
                            Clipboard.SetText(lastMsg);
                            //MessageBox.Show(lastMsg);
                            this.Text = this.Text.Replace("[Listening]", "");
                            tmrDelayTick(null, null);
                            return;
                        }else if (steControl == 2 && eCell != null)
                        {
                            //isSelfShortcut = true;
                            //tmrDelay.Tag = MouseData;
                            //tmrDelayTick(null, null);
                            TextBox ed = ((TextBox)dataView.EditingControl);
                            int caret = ed.SelectionStart;
                            int selStart = ed.SelectionStart, selEnd = ed.SelectionStart + ed.SelectionLength;
                            ed.Text = ed.Text.Substring(0, selStart) + ed.Text.Substring(selEnd, ed.Text.Length - selEnd);  //remove selection
                            ed.Text = ed.Text.Insert(caret, MouseData);
                            ed.Select(caret + MouseData.Length, 0);
                            return;
                        }
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    case Keys.T:
                        //if (!listening)
                        //{
                        //    MessageBox.Show(tmrAsync.Enabled.ToString());
                        //    tmrAsync.Enabled = !tmrAsync.Enabled;
                        //}
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    //case Keys.R:
                    //    if (steControl == 2 && steShift == 2 && !lastMsg.Equals(""))
                    //    {
                    //        tmrDelay.Tag = lastMsg;
                    //        routineRepeats = lastRepeats;
                    //        steE = 2;
                    //        isRepeat = true;
                    //        Clipboard.SetText(lastMsg);
                    //        //MessageBox.Show(lastMsg);
                    //        tmrDelayTick(null, null);
                    //        return;
                    //    }
                    //    //steShift = 0;
                    //    //steControl = 0;
                    //    break;
                    case Keys.Enter:
                        //MessageBox.Show("here");
                        if(!codeTag.Equals(""))
                        {
                            checkTags(true);
                            //steShift = 0;
                            //steControl = 0;
                            return;
                        }
                        //steShift = 0;
                        //steControl = 0;
                        break;
                    default:
                        //steShift = 0;
                        //steControl = 0;
                        break;
                }

                if (steControl == 2 && steShift == 2)
                {
                    codeTag = "";
                    routineRepeats = "";
                    bkspForRepeats = 0;
                    this.listening = true;
                    if (!this.Text.Contains("[Listening]") && !typing)
                        this.Text += "[Listening]";
                    tmrDelay.Tag = "";
                    return;
                }

                if (this.listening && !typing)
                {
                    string s = kc.ConvertToString(k);
                    s = s.Replace("NumPad", "");
                    if (s.Equals("Subtract") || s.Equals("OemMinus"))
                    {
                        s = "-";

                    }else if(s.Equals("Add"))
                    {
                        s = "+";
                    }else if(s.Equals("Multiply"))
                    {
                        s = "*";
                    }else if(s.Equals("Divide"))
                    {
                        s = "/";
                    }else if(s.Equals("OemPlus"))
                    {
                        s = "=";
                    }
                    //MessageBox.Show(s);
                    if (s.Length == 1 || s.Equals("Space"))
                    {
                        codeTag += s.ToUpper();
                        checkTags(false);
                    }
                }
            }
        }

        public void KeyupIntercepted(Keys k)
        {
            lock (synLock)
            {
                //MessageBox.Show(k.ToString());
                switch (k)
                {
                    case Keys.LShiftKey:
                        steShift = 0;
                        this.Text = this.Text.Replace("[^]", "");
                        this.Text = this.Text.Replace("Listening R", "Listening");
                        listeningRepeats = false;
                        break;
                    case Keys.LControlKey:
                        steControl = 0;
                        this.Text = this.Text.Replace("[#]", "");
                        if (steShift == 2)
                        {
                            //MessageBox.Show("here");
                            this.Text = this.Text.Replace("Listening", "Listening R");
                            listeningRepeats = true;
                        }
                        break;
                    case Keys.Escape:
                        this.Text = this.Text.Replace("[X]", "");
                        break;
                    case Keys.Multiply:
                        steMult = 0;
                        break;
                    case Keys.E:
                        steE = 0;
                        break;
                        //case Keys.S:
                        //    if (steControl == 2)
                        //    {
                        //        steControl = 1;
                        //        this.Text = this.Text.Replace("[#]", "");
                        //    }
                        //    if (steShift == 2)
                        //    {
                        //        steShift = 1;
                        //        this.Text = this.Text.Replace("[^]", "");
                        //    }
                        //    break;
                }
            }
        }

        private void checkTags(bool runOnFound)
        {
            //MessageBox.Show(codeTag);

            Boolean others = false;
            Boolean found = false;
            string msg = "";

            foreach (DataGridViewRow r in dataView.Rows)
            {
                if (r.Cells[0].Value == null)
                    continue;

                if (r.Cells[0].Value.ToString().ToUpper().StartsWith(codeTag))
                {
                    found = true;
                    if (r.Cells[0].Value.ToString().ToUpper().Equals(codeTag) && r.Cells[dataView.ColumnCount - 1].Value != null)
                    {
                        msg = r.Cells[dataView.ColumnCount - 1].Value.ToString();
                    }
                    else
                    {
                        others = true;
                    }
                }
            }

            foreach (DataGridViewRow r in searchExcludedRows)
            {
                if (r.Cells[0].Value == null)
                    continue;
                //MessageBox.Show(r.Cells[0].Value.ToString());
                if (r.Cells[0].Value.ToString().ToUpper().StartsWith(codeTag))
                {
                    found = true;
                    if (r.Cells[0].Value.ToString().ToUpper().Equals(codeTag))
                    {
                        msg = r.Cells[dataView.ColumnCount - 1].Value.ToString();
                    }
                    else
                    {
                        others = true;
                    }
                }
            }
            if (found)
            {
                if (!msg.Equals(""))
                {
                    tmrDelay.Tag = msg;
                    if (others && !runOnFound)
                    {
                        tmrDelay.Start();
                        return;
                    }
                    else
                    {
                        //MessageBox.Show(msg);
                        tmrDelay.Stop();
                        if (runOnFound)
                        {
                            sendKey((char)206); //bkspc for enter
                        }
                        this.Text = this.Text.Replace("[Listening]", "");
                        tmrDelayTick(null, null);
                        return;
                    }
                }
            }

            if (!found || runOnFound)
            {
                lock (synLock)
                {
                    listening = false;
                    this.Text = this.Text.Replace("[Listening]", "");
                    steShift = 0;
                    steControl = 0;
                    codeTag = "";
                    routineRepeats = "";
                    tmrDelay.Stop();
                }
                System.Media.SystemSounds.Beep.Play();
            }
        }

        private void tmrDelayTick(object sender, EventArgs e)
        {
            //MessageBox.Show(steControl.ToString());
            lock (synLock)
            {
                this.listening = false;
                this.Text = this.Text.Replace("[Listening]", "");
                if (tmrDelay.Tag == null)
                    return;

                this.Text += "[Working]";
                tmrAsyncDataList.AddLast(new tmrAsyncData(tmrAsyncDataTypes.CURSORWAIT, null));
            }
            //MessageBox.Show(steControl.ToString());
            //snd.play();

            string msg = (string)tmrDelay.Tag;

            wrkTyper = new BackgroundWorker();
            wrkTyper.DoWork += new DoWorkEventHandler(this.wrkTyper_DoWork);
            msg = msg.Replace("@|!date", getDate());
            wrkTyper.RunWorkerAsync(msg);
            tmrDelay.Stop();
        }

        private void wrkTyper_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //MessageBox.Show(steControl.ToString());
                while (true)
                {
                    lock (synLock)
                    {
                        if (steShift < 2 && steControl < 2 && steE < 2)
                        {
                            steShift = 0;
                            steControl = 0;
                            listening = false;
                            //this.Text = this.Text.Replace("[Listening]", "");
                            typing = true;
                            //if (!this.Text.Contains("[Typing]"))
                            //    this.Text += "[Typing]";

                            if (!isSelfShortcut)
                            {
                                lastMsg = ((string)e.Argument);        //info for repeats
                                lastRepeats = routineRepeats;
                            }
                            break;
                        }
                    }
                }

                //MessageBox.Show(lastMsg);
                int sleepTime = altT;
                string msg = ((string)e.Argument);
                //MessageBox.Show(msg);
                char[] msgArr = msg.ToCharArray();
                System.Threading.Thread.Sleep(sleepTime);

                if (!isRepeat)
                {
                    for (int i = 0; i < codeTag.Length + bkspForRepeats; i++)
                    {
                        SendKeys.SendWait("{BACKSPACE}");
                        //System.Threading.Thread.Sleep(10);
                    }
                    codeTag = "";
                    bkspForRepeats = 0;
                }

                //MessageBox.Show(msg);
                if (chkInstant.Checked)
                {
                    if (routineRepeats.Equals(""))
                    {
                        routineRepeats = "1";
                    }
                    int repeats = int.Parse(routineRepeats);
                    //MessageBox.Show(repeats.ToString());
                    while (repeats > 0)
                    {                   
                        //MessageBox.Show(5.ToString());
                        if (eCell != null)
                        {
                            TextBox ed = ((TextBox)dataView.EditingControl);
                            int caret = ed.SelectionStart;
                            int selStart = ed.SelectionStart, selEnd = ed.SelectionStart + ed.SelectionLength;
                            ed.Text = ed.Text.Substring(0, selStart) + ed.Text.Substring(selEnd, ed.Text.Length - selEnd);  //remove selection
                            ed.Text = ed.Text.Insert(caret, msg);
                            ed.Select(caret + msg.Length, 0);
                            continue;
                        }

                        int start = 0, end = msg.IndexOf("@|!");
                        //MessageBox.Show(end.ToString());
                        while (end != -1)
                        {
                            //MessageBox.Show(start.ToString() + ", " + end.ToString());
                            //MessageBox.Show(msg.Substring(start, end - start));
                            //paste txt
                            if (msg.Substring(start, end - start).Length != 0)
                            {
                                Invoke((Action)(() => { Clipboard.SetText(msg.Substring(start, end - start)); }));
                                SendKeys.SendWait("^v");
                            }

                            //execute cmd (if there)
                            switch (msg.Substring(end + 3, 1).ToCharArray()[0]) //get cmd char
                            {
                                case 'M':    //mouse
                                    end = sendMouse(msg, end + 4);  // -> updates position in msg
                                    break;
                                case 'W':    //wait
                                    end = sendWait(msg, end + 4);
                                    break;
                                case 'S':   //not implemented
                                    end = sendShell(msg, end + 4);
                                    break;
                                case '_':   //printscreen
                                    end = playRecording(msg, end + 4);
                                    break;
                                default:    //other
                                    //MessageBox.Show(end.ToString() + ", " + start.ToString());
                                    end = sendCmd(msg, end + 3);
                                    break;
                            }
                            start = end + 1;
                            end = msg.IndexOf("@|!", start);

                            //MessageBox.Show(end.ToString() + ", " + start.ToString());

                            //check stop
                            lock (synLock)
                            {
                                //if (i == 0)
                                //MessageBox.Show(stop.ToString());
                                if (stop)
                                {
                                    stop = false;
                                    workEnd();
                                    return;
                                }
                            }
                        }

                        //MessageBox.Show("sdf");

                        //paste last bit
                        if (msg.Substring(start, msg.Length - start).Length != 0)
                        {
                            Invoke((Action)(() => { Clipboard.SetText(msg.Substring(start, msg.Length - start)); }));
                            SendKeys.SendWait("^v");
                        }

                        repeats--;
                    }
                }
                else
                {
                    //MessageBox.Show(routineRepeats);
                    //int macroFlag = 0;
                    //string flag = "";
                    if (routineRepeats.Equals(""))
                    {
                        routineRepeats = "1";
                    }
                    int repeats = int.Parse(routineRepeats);
                    //MessageBox.Show(repeats.ToString());
                    while (repeats > 0)
                    {
                        Boolean newline = false;
                        int tagIdx = 0;
                        for (int i = 0; i < msgArr.Length; i++)
                        {
                            //if (i == 0)
                            //    MessageBox.Show(repeats.ToString());

                            lock (synLock)
                            {
                                //if (i == 0)
                                //MessageBox.Show(stop.ToString());
                                if (stop)
                                {
                                    stop = false;
                                    workEnd();
                                    return;
                                }
                            }

                            //if (i == 0)
                            //    MessageBox.Show(repeats.ToString());
                            //System.Threading.Thread.Sleep(2);

                            if (msgArr[i] == '\r')
                            {
                                newline = true;
                                continue;
                            }
                            else if (newline && msgArr[i] == '\n')
                            {
                                System.Threading.Thread.Sleep(1000 / speed);
                                SendKeys.SendWait("{ENTER}");
                                newline = false;
                                continue;
                            }

                            //MessageBox.Show(msgArr[i].ToString() + ", "+ tagIdx.ToString());
                            switch (tagIdx)
                            {
                                case 0:
                                    if (msgArr[i] == '@')    //@
                                    {
                                        if (i == msgArr.Length - 0)
                                            sendKey('@');
                                        tagIdx++;
                                        continue;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                case 1:
                                    if (msgArr[i] == '|')   //|
                                    {
                                        if (i >= msgArr.Length - 1)
                                        {
                                            sendKey('@');
                                            sendKey('|');
                                        }
                                        tagIdx++;
                                        continue;
                                    }
                                    else
                                    {
                                        //MessageBox.Show(msgArr[i].ToString() + tagIdx.ToString());
                                        sendKey('@');
                                        tagIdx = 0;
                                        i--;
                                        continue;
                                    }
                                case 2:
                                    if (msgArr[i] == '!')    //!
                                    {

                                        if (i >= msgArr.Length - 2)
                                        {
                                            sendKey('@');
                                            sendKey('|');
                                            sendKey('!');
                                        }

                                        tagIdx++;
                                        continue;
                                    }
                                    else if (msgArr[i] == '`')
                                    {
                                        sendKey('@');
                                        sendKey('|');
                                        sendKey('!');
                                        tagIdx = 0;
                                        continue;
                                    }
                                    else
                                    {
                                        sendKey('@');
                                        tagIdx = 0;
                                        i-=2;
                                        continue;
                                    }
                            }

                            if (tagIdx == 3)
                            {
                                //MessageBox.Show(msgArr[i].ToString());
                                switch (msgArr[i])
                                {
                                    case 'M':    //m
                                        i = sendMouse(msg, i + 1);  // -> updates position in msg
                                        tagIdx = 0;
                                        continue;
                                    case 'W':    //wait
                                        i = sendWait(msg, i + 1);
                                        tagIdx = 0;
                                        continue;
                                    case 'S':   //shell
                                        i = sendShell(msg, i + 1);
                                        tagIdx = 0;
                                        continue;
                                    case '_':   //play Rec
                                        i = playRecording(msg, i + 1);
                                        tagIdx = 0;
                                        continue;
                                    default:    //other
                                        i = sendCmd(msg, i);
                                        tagIdx = 0;
                                        continue;
                                }
                            }

                            sendKey(msgArr[i]);
                            //MessageBox.Show(((int)c).ToString());
                            // special chars

                            //MessageBox.Show(c.ToString());
                        }
                        repeats--;
                    }
                }

                //if (altTab)
                //{
                //    System.Threading.Thread.Sleep(sleepTime);
                //    SendKeys.SendWait("%{TAB}");
                //}

                //SendKeys.SendWait("{BACKSPACE}");
                //MessageBox.Show("sdf");
            }catch(Exception ee)
            {
                MessageBox.Show(ee.Message + "\n\n" + ee.StackTrace);
            }
            workEnd();
        }

        private void workEnd()
        {
            //MessageBox.Show("ended");
            //already called from a lock block
            typing = false;
            routineRepeats = "";
            steShift = 0;
            steControl = 0;
            steE = 0;
            tmrDelay.Tag = null;
            isRepeat = false;
            isSelfShortcut = false;

            //MessageBox.Show(this.Text.Replace("[Working]", ""));
            tmrAsyncData d = new tmrAsyncData();
            d.action = tmrAsyncDataTypes.REMOVEFROMTITLE;
            this.Tag = "[Working]";     // look at tick method -> needs this in wndMain tag
            d.value = this;
            tmrAsyncDataList.AddLast(d);
            tmrAsyncDataList.AddLast(new tmrAsyncData(tmrAsyncDataTypes.CURSORDEFAULT, null));

            //MessageBox.Show(steControl.ToString());
            //MessageBox.Show(steControl.ToString());
        }

        private void sendKey(char c)
        {
            switch ((int)c)
            {
                case 43:    //+
                case 94:    //^
                case 37:    //%
                case 126:   //~
                case 123:   //{
                case 125:   //}
                case 40:    //(
                case 41:    //)
                case 91:    //[
                case 93:    //]
                    SendKeys.SendWait("{" + c + "}");
                    break;
                case 11:    //TAB
                    SendKeys.SendWait("{TAB}");
                    break;
                //case 201:   //RIGHT
                //    SendKeys.SendWait("{RIGHT}");
                //    break;
                //case 202:   //LEFT
                //    SendKeys.SendWait("{LEFT}");
                //    break;
                //case 203:   //UP
                //    SendKeys.SendWait("{UP}");
                //    break;
                //case 204:   //DOWN
                //    SendKeys.SendWait("{DOWN}");
                //    break;
                //case 205:
                //    SendKeys.SendWait("{ENTER}");
                //    break;
                //case 206:
                //    SendKeys.SendWait("{BACKSPACE}");
                //    break;
                //case 207:
                //    SendKeys.SendWait("^c");
                //    break;
                //case 208:
                //    SendKeys.SendWait("^v");
                //    break;
                default:
                    SendKeys.SendWait(c.ToString());
                    break;
            }
            System.Threading.Thread.Sleep(1000 / speed);
        }

        //returns position after function block
        private int sendCmd(string info, int index)
        {
            int ori = index;
            try
            {
                //MessageBox.Show(index.ToString() + " " + info);
                int end = info.IndexOf(";", index);
                char[] crudeMsg = info.Substring(index, end - index).ToCharArray();
                string cmd = "";
                for (int i = 0; i < crudeMsg.Length; i++)
                {                    
                    switch (crudeMsg[i])
                    {
                        case 'F':
                            cmd += "{F" + crudeMsg[i + 1];  //f and then the number                            
                            int secondDigit = 0;
                            if (i+2 < crudeMsg.Length && int.TryParse(crudeMsg[i + 2].ToString(), out secondDigit))            // if 2 digits like f12
                            {                               
                                cmd += crudeMsg[i + 2] + "}";
                                i+=2;
                            }
                            else
                            {
                                cmd += "}";
                                i++;
                            }
                            break;
                        case '^':   //shift
                            cmd += "+";
                            break;
                        case '#':   //ctrl
                            cmd += "^";
                            break;
                        case '@':   //alt
                            cmd += "%";
                            break;
                        case 'I':   //insert
                            cmd += "{INS}";
                            break;
                        case '*':
                            cmd += "{DEL}";
                            break;
                        case 'H':
                            cmd += "{HOME}";
                            break;
                        case 'N':
                            cmd += "{END}";
                            break;
                        case '<':
                            cmd += "{PGUP}";
                            break;
                        case '>':
                            cmd += "{PGDN}";
                            break;
                        case 'T':
                            cmd += "{TAB}";
                            break;
                        case 'E':   //enter
                            cmd += "~";
                            break;
                        case 'B':   //backspace
                            cmd += "{BS}";
                            break;
                        case 'C':
                            cmd += "{CAPSLOCK}";
                            break;
                        case 'X':
                            myEscapes++;    //IMPORTANT!!!
                            cmd += "{ESC}";
                            break;
                        case 'U':
                            cmd += "{UP}";
                            break;
                        case 'D':
                            cmd += "{DOWN}";
                            break;
                        case 'L':
                            cmd += "{LEFT}";
                            break;
                        case 'R':
                            cmd += "{RIGHT}";
                            break;
                        case '?':
                            cmd += "{HELP}";
                            break;
                        case 'P':   //printscreen
                            cmd += "{PRTSC}";
                            break;
                        case ';':
                            break;
                        default:
                            cmd += crudeMsg[i];
                            break;
                    }
                }
                //string final = "";

                //// to sendKey format
                //final += cmd.Replace("^", "+");    //shift
                //final += cmd.Replace("#", "^");    //ctrl
                //final += cmd.Replace("@", "%");    //alt

                //final += cmd.Replace("I", "{INS}");        //insert
                //final += cmd.Replace("*", "{DEL}");        //del

                //final += cmd.Replace("H", "{HOME}");       //home
                //final += cmd.Replace("N", "{END}");        //end

                //final += cmd.Replace("<", "{PGUP}");       //pg up
                //final += cmd.Replace(">", "{PGDN}");       //pg dn

                //final += cmd.Replace("T", "{TAB}");        //tab
                //final += cmd.Replace("E", "~");            //enter
                //final += cmd.Replace("B", "{BS}");         //bkspace
                //final += cmd.Replace("C", "{CAPSLOCK}");   //capslock
                //final += cmd.Replace("X", "{ESC}");        //esc

                //final += cmd.Replace("U", "{UP}");         //up
                //final += cmd.Replace("D", "{DOWN}");       //dn
                //final += cmd.Replace("L", "{LEFT}");       //L
                //final += cmd.Replace("R", "{RIGHT}");      //R

                //final += cmd.Replace("?", "{HELP}");       //
                //final += cmd.Replace("P", "{PRTSC}");      //PRINT SCREEN

                //MessageBox.Show(info.Substring(end));
                //MessageBox.Show(cmd);
                if (!chkInstant.Checked)
                    System.Threading.Thread.Sleep(1000 / speed);
                SendKeys.SendWait(cmd);
                return end;
            }
            catch (Exception ignored)
            {
                MessageBox.Show("CHECK THE ROUTINE! " + ignored.ToString());
                return info.IndexOf(";", index);
            }
        }

        private int sendShell(string info, int index)
        {
            try
            {
                int mid = info.IndexOf(" - ", index);
                int end = info.IndexOf(";", index);
                string name = "", args = "";
                if (mid != -1)
                {
                    name = info.Substring(index, mid - index);
                    args = info.Substring(mid + 2, end - mid - 2);
                }
                else
                {
                    name = info.Substring(index, end - index);
                }
                //name = name.Replace("\\", "\\\\");
                //MessageBox.Show(name);
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = args,
                        UseShellExecute = true,
                        RedirectStandardOutput = false,
                        CreateNoWindow = false
                    }
                };
                proc.Start();
                //MessageBox.Show(info.Substring(end));
                return end;
            }
            catch (Exception ignored)
            {
                MessageBox.Show("CHECK THE ROUTINE! format: [@|!Sname - args;]\n" + ignored.ToString());
                return info.IndexOf(";", index) + 1;
            }
        }

        //returns position after function block
        private int sendMouse(string info, int index)
        {
            int ori = index;
            try
            {
                //MessageBox.Show(index.ToString());
                //get btn
                char btn = info.Substring(index, 1).ToCharArray()[0];
                const char L = 'L', R = 'R';
                index++;

                //get pos
                int x, y;
                int start = info.IndexOf("(", index) + 1;
                int end = info.IndexOf(",", index);
                string temp = info.Substring(start, end - start);
                int.TryParse(temp, out x);
                x = (int) Math.Round((x+0.0) / zoom);

                start = info.IndexOf(",", index) + 2;  //2 since space
                end = info.IndexOf(")", index);
                temp = info.Substring(start, end - start);
                int.TryParse(temp, out y);
                y = (int)Math.Round((y + 0.0) / zoom);

                int action = 6;
                //MessageBox.Show(end.ToString());
                //MessageBox.Show((info.IndexOf(";", index) - 1).ToString());
                if (end == info.IndexOf(";", index) - 2)
                {
                    if(info.Substring(end+1, 1).Equals("D"))
                    {
                        action = 2; //down only
                    }else if(info.Substring(end+1, 1).Equals("U"))
                    {
                        action = 3; //up only
                    }
                }

                end = info.IndexOf(";", index);

                //MessageBox.Show(btn.ToString());
                //MessageBox.Show(info);
                //MessageBox.Show(x.ToString() + ", " + y.ToString());

                if(speed < 250 && !chkInstant.Checked)
                {
                    int x1 = Cursor.Position.X, y1 = Cursor.Position.Y;
                    //MessageBox.Show(Cursor.Position.ToString());
                    int dx = x - x1, dy = y - y1;
                    long interval = (long)7500 / speed;
                    long initialT = currentTimeMilis();
                    long elapsedT = 0;
                    long lastT = initialT - 16;     // - 16 for first run
                    if (!chkInstant.Checked)
                    {
                        while (currentTimeMilis() - initialT < interval)
                        {
                            if (currentTimeMilis() - lastT > 15)
                            {
                                lock (synLock)
                                {
                                    //if (i == 0)
                                    //MessageBox.Show(stop.ToString());
                                    if (stop)
                                    {
                                        return end;
                                    }
                                }
                                elapsedT = currentTimeMilis() - initialT;
                                //MessageBox.Show((elapsedT).ToString());
                                //MessageBox.Show((interval).ToString());
                                //MessageBox.Show((elapsedT*1.0 / interval*1.0).ToString());
                                //MessageBox.Show((1.0 / (5.0 * (elapsedT * 1.0 / interval * 1.0) + 0.854102) - 0.17082).ToString());
                                SetCursorPos(Convert.ToInt32(x - dx * (1.0 / (Math.Pow(250, (elapsedT * 1.0 / interval * 1.0))) - 0.004)),
                                    Convert.ToInt32(y - dy * (1.0 / (Math.Pow(250, (elapsedT * 1.0 / interval * 1.0))) - 0.004)));
                                lastT = currentTimeMilis();
                            }
                        }
                    }
                }
                SetCursorPos(x, y);

                int ev = 0, evUp = 0;
                switch (btn)
                {
                    case L:
                        ev = MOUSEEVENTF_LEFTDOWN;
                        evUp = MOUSEEVENTF_LEFTUP;
                        break;
                    case R:
                        ev = MOUSEEVENTF_RIGHTDOWN;
                        evUp = MOUSEEVENTF_RIGHTUP;
                        break;
                    default:
                        return end;
                }

                //System.Threading.Thread.Sleep(20);
                if(action % 2 == 0)
                {
                    mouse_event(ev, 0, 0, 0, 0);
                }
                if (action % 3 == 0)
                {
                    mouse_event(evUp, 0, 0, 0, 0);
                }

                //MessageBox.Show(temp);
                //MessageBox.Show(x.ToString() + ", " + y.ToString());
                //MessageBox.Show(index.ToString());
                if(!chkInstant.Checked)
                    System.Threading.Thread.Sleep(1000 / speed);

                return end;

            }
            catch (Exception ignored)
            {
                MessageBox.Show("CHECK THE ROUTINE! " + ignored.ToString());
                return info.IndexOf(";", index);
            }
        }

        private int sendWait(string info, int index)
        {
            int ori = index;
            try
            {
                int end = info.IndexOf(";", index);
                string temp = info.Substring(index, end - index);
                int interval = 0;
                int.TryParse(temp, out interval);
                if (interval < 0)
                    interval = 0;
                System.Threading.Thread.Sleep(interval);
                //MessageBox.Show(info.Substring(end));
                return end;
            }
            catch (Exception ignored)
            {
                MessageBox.Show("CHECK THE ROUTINE! " + ignored.ToString());
                return info.IndexOf(";", index) + 1;
            }
        }

        private int playRecording(string info, int index)
        {
            int ori = index;
            try
            {
                int end = info.IndexOf(";", index);
                string temp = info.Substring(index, end - index);
                int interval = 0;
                int.TryParse(temp, out interval);
                if (interval < 0)
                    interval = 0;
                System.Threading.Thread.Sleep(interval);
                //MessageBox.Show(info.Substring(end));
                return end;
            }
            catch (Exception ignored)
            {
                MessageBox.Show("CHECK THE ROUTINE! needs [@|!_recording.txt;]" + ignored.ToString());
                return info.IndexOf(";", index) + 1;
            }
        }

        private void wndMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock(synLock)
            {
                if(!saved)
                    if (MessageBox.Show(this,
                        "Save?", "Autokeys", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        save();
            }
        }

        private void save()
        {

            dataView.EndEdit();

            lock (synLock)
            {
                string save = "";

                // column widths
                for (int i = 0; i < dataView.ColumnCount; i++)
                {
                    save += dataView.Columns[i].Width;
                    save += "\r\n";
                }

                save += dataView.Font.Size.ToString() + "\r\n";
                save += tmrDelay.Interval.ToString() + "\r\n";
                save += altT.ToString() + "\r\n";
                save += chkInstant.Checked.ToString() + "\r\n";
                save += speed.ToString() + "\r\n";
                save += zoom.ToString() + "\r\n";

                //colors
                foreach (int color in cd.CustomColors)
                {
                    save += color.ToString() + "\r\n";
                }

                // data
                foreach (DataGridViewRow r in dataView.Rows)
                {
                    save = saveRow(r, save);
                }

                // erase last line
                save = save.Substring(0, save.Length - 5*(dataView.ColumnCount+1) - 4);

                // add hidden by search
                foreach (DataGridViewRow r in searchExcludedRows)
                {
                    save = saveRow(r, save);
                }

                File.WriteAllText(saveFile, save);

                saved = true;
            }
        }

        private string saveRow(DataGridViewRow r, string save)
        {
            if (r.Tag == null)
                r.Tag = "0";

            string msg = "";
            if (r.DefaultCellStyle.BackColor == Color.LightGray && r.Cells[3].Value == null)
                return save;

            int clms = dataView.ColumnCount - 1;
            for (int i = 0; i < clms; i ++)
            {
                save += r.Cells[i].Value + " @|" + (i+1).ToString() + " ";
            }

            msg = (string)r.Cells[dataView.ColumnCount - 1].Value;
            if (msg != null)
            {
                msg = msg.Replace("\r\n", "@|nl");
                msg = msg.Replace("\n", "@|nl");
            }
            save += msg + " @|" + dataView.ColumnCount + " ";
            save += r.Tag.ToString() + " @|" + (dataView.ColumnCount + 1) + " ";
            save += "\r\n";

            return save;
        }

        private void chkInstant_CheckedChanged(object sender, EventArgs e)
        {
            lock (synLock)
            {
                saved = false;
            }
        }

        //private void runTest(Object o)
        //{
        //    SendKeys.Send("%{f4}");
        //    try
        //    {
        //        if (this.Text.Contains("("))
        //        {
        //            int start = this.Text.IndexOf("(");
        //            int end = this.Text.IndexOf(")");
        //            //MessageBox.Show(start.ToString() + " " +  end.ToString());
        //            string replace = this.Text.Substring(start, end - start + 1);
        //            //MessageBox.Show(replace);
        //            this.Text = this.Text.Replace(replace, "");
        //        }
        //        Point p = (Point)o;
        //        this.Text += "(" + p.X.ToString() + ", " + p.Y.ToString() + ")";
        //        //string msg = "";
        //        //msg += steControl.ToString() + ", " + steShift.ToString();
        //        //msg += "\r\n" + ">" + codeTag + "<";
        //        //MessageBox.Show(msg);
        //    }
        //    catch (Exception e)
        //    {

        //    }

        //}

        private void txtSpeed_Leave(object sender, EventArgs e)
        {
            lock (synLock)
            {
                saved = false;
            }

            if (txtSpeed.Text.Equals(""))
            {
                txtSpeed.Text = speed.ToString();
                return;
            }

            try
            {
                speed = Convert.ToInt32(txtSpeed.Text);
                if (speed < 1)
                {
                    speed = 1;
                    txtSpeed.Text = speed.ToString();
                    txtSpeed.SelectionStart = txtSpeed.Text.Length;
                }
            }
            catch (Exception x)
            {
                txtSpeed.Text = speed.ToString();
                txtSpeed.SelectionStart = txtSpeed.Text.Length;
            }
        }

        private void txtAltTab_Leave(object sender, EventArgs e)
        {
            lock (synLock)
            {
                saved = false;
            }

            if (txtAltTab.Text.Equals(""))
            {
                txtAltTab.Text = altT.ToString();
                return;
            }

            try
            {
                altT = Convert.ToInt32(txtAltTab.Text);
            }
            catch (Exception x)
            {
                txtAltTab.Text = altT.ToString();
            }
        }

        private void txtFont_Leave(object sender, EventArgs e)
        {
            lock (synLock)
            {
                saved = false;
            }

            float Size = dataView.Font.Size;
            try
            {
                Size = Convert.ToSingle(txtFont.Text);
                dataView.Font = new Font(dataView.Font.FontFamily, Size);
            }
            catch (Exception x)
            {
                txtFont.Text = Size.ToString();
            }
        }

        private void tmrAsyncTick(object sender, EventArgs e)
        {
            //MessageBox.Show("as");
            //get first action in list and remove from list -> else stop
            lock (synLock)
            {
                if (tmrAsyncDataList.Count == 0)
                {
                    return;
                    //MessageBox.Show("2");
                }
                //MessageBox.Show("1");

                tmrAsyncData d = tmrAsyncDataList.First.Value;
                tmrAsyncDataList.RemoveFirst();

                //MessageBox.Show("2");
                switch (d.action)
                {
                    case tmrAsyncDataTypes.REMOVEFROMTITLE: // <-
                        //MessageBox.Show("2");
                        Form w = (Form)d.value;
                        w.Text = w.Text.Replace((string)w.Tag, "");
                        break;
                    case tmrAsyncDataTypes.REMOVEEMPTYROW:  // <-
                        int index = (int)d.value;
                        try
                        {
                            dataView.Rows.RemoveAt(index);
                        }
                        catch (Exception ee)
                        {
                            tmrAsyncDataList.AddLast(d);
                        }
                        break;
                    case tmrAsyncDataTypes.CURSORWAIT:
                        Cursor.Current = Cursors.WaitCursor;
                        break;
                    case tmrAsyncDataTypes.CURSORDEFAULT:
                        Cursor.Current = Cursors.Default;
                        break;
                }
            }
        }

        private void pnlSearch_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtZoom_Leave(object sender, EventArgs e)
        {
            lock (synLock)
            {
                saved = false;
            }

            if (txtZoom.Text.Equals(""))
            {
                txtZoom.Text = zoom.ToString();
                return;
            }

            try
            {
                zoom = Convert.ToSingle(txtSpeed.Text);
                if (zoom < 0)
                {
                    zoom = 0f;
                    txtZoom.Text = zoom.ToString();
                    txtZoom.SelectionStart = txtZoom.Text.Length;
                }
            }
            catch (Exception x)
            {
                txtZoom.Text = zoom.ToString();
                txtZoom.SelectionStart = txtZoom.Text.Length;
            }
        }

        private void txtDelay_Leave(object sender, EventArgs e)
        {
            lock (synLock)
            {
                saved = false;
            }

            int delay = tmrDelay.Interval;
            try
            {
                delay = Convert.ToInt32(txtDelay.Text);
                tmrDelay.Interval = delay;
            }
            catch (Exception x)
            {
                txtDelay.Text = delay.ToString();
            }
        }
    }
}