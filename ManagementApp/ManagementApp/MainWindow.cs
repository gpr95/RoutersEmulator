﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ManagementApp
{
    public partial class MainWindow : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);
        //private static extern bool ShowWindow(IntPtr hWnd, int  nCmdShow);
        // CONSTS
        private const int GAP = 10;
        private readonly int CLOUDPORT = 7776;
        // LOGICAL VARS
        private OperationType oType;
        private ManagementPlane management;
        private DataTable table;
        private List<Node> nodeList;
        private List<NodeConnection> connectionList;
        private List<NodeConnection> connectionTemp = new List<NodeConnection>();
        private List<Domain> domainList;
        private CloudCableHandler cableHandler;
        private List<Trail> tempTrailList = new List<Trail>();

        // PAINTING VARS
        private bool isDrawing = false;
        private Node aNode;
        private Node bNode;
        private Node nodeFrom;
        private Node virtualNodeTo;
        private Bitmap containerPoints;
        private Point domainFrom;
        private Graphics myGraphics;

        internal ManagementPlane Management
        {
            get
            {
                return management;
            }

            set
            {
                management = value;
            }
        }

        enum OperationType
        {
            ADD_CLIENT_NODE,
            ADD_NETWORK_NODE,
            ADD_CONNECTION,
            ADD_DOMAIN,
            DELETE,
            MOVE_NODE,
            NOTHING
        }

        public MainWindow(DataTable table, List<Node> nodeList, List<NodeConnection> connectionList, List<Domain> domainList)
        {
            //TODO: start chmury kablowej
            cableHandler = new CloudCableHandler(connectionList, CLOUDPORT);
            InitializeComponent();
            hidePortSetup();
            RenderTable();
            this.table = table;
            this.nodeList = nodeList;
            this.connectionList = connectionList;
            this.domainList = domainList;

        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            containerPoints = new Bitmap(containerPictureBox.ClientSize.Width, containerPictureBox.ClientSize.Height);
            for (int x = 0; x < containerPictureBox.ClientSize.Width;
                x += GAP)
            {
                for (int y = 0; y < containerPictureBox.ClientSize.Height;
                    y += GAP)
                {
                    containerPoints.SetPixel(x, y, Color.Gainsboro);
                }
            }
            myGraphics = containerPictureBox.CreateGraphics();
            myGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        }

        private void hidePortSetup()
        {
            label1.Visible = false;
            label2.Visible = false;
            textBox1.Visible = false;
            textBox2.Visible = false;
            button2.Visible = false;
        }

        private void showPortSetup(Node from, Node to)
        {
            
            aNode = from;
            bNode = to;
            containerPictureBox.Update();
            if (from == null)
                return;
            if (to == null)
                return;
            label1.Text = aNode.Name + " port:";
            label2.Text = bNode.Name + " port:";
            
            label1.Visible = true;
            label2.Visible = true;
            textBox1.Visible = true;
            textBox2.Visible = true;
            button2.Visible = true;
        }

        private void RenderTable()
        {
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void containerPictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics panel = e.Graphics;

            foreach (var elem in domainList)
            {
                drawElement(elem, panel);
            }
            foreach (var elem in connectionList)
            {
                drawConnection(elem, panel);
            }
            foreach (Trail t in tempTrailList)
            {
                drawTrail(t, panel);
            }
            foreach (var node in nodeList)
            {
                drawNode(node, panel);
            }
            containerPictureBox.BackgroundImage = containerPoints;
        }



        private void containerPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            int x = e.X;
            int y = e.Y;
            putToGrid(ref x, ref y);
            switch (oType)
            {
                case OperationType.ADD_CLIENT_NODE:
                    management.addClientNode(x, y);
                    break;
                case OperationType.ADD_NETWORK_NODE:
                    management.addNetworkNode(x, y);
                    break;
                case OperationType.DELETE:
                    deleteListBox.Visible = false;
                    deleteListBox.Enabled = false;
                    deleteListBox.Items.Clear();
                    containerPictureBox.Refresh();

                    Node n = getNodeFrom(x, y);
                    if (n == null)
                        break;
                    List<String> atPosition = management.findElemAtPosition(x, y);

                    foreach (String toDelete in atPosition)
                        deleteListBox.Items.Add(toDelete);
                    if (deleteListBox.Visible.Equals(true) || deleteListBox.Items == null)
                        break;
                    if (atPosition.Count > 1)
                    {
                        deleteListBox.Items.Add("Restart " + atPosition.Last());
                        deleteListBox.Items.Add("Cancel");
                        deleteListBox.Location = new Point(x, y);
                        deleteListBox.Visible = true;
                        deleteListBox.Enabled = true;
                        autofit();
                    }
                    else if (atPosition.Count == 1)
                    {
                        management.deleteNode(n);
                    }
                    break;

            }
            containerPictureBox.Refresh();
        }

        private void containerPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            int x = e.X;
            int y = e.Y;
            putToGrid(ref x, ref y);
            switch (oType)
            {
                case OperationType.ADD_CONNECTION:
                    nodeFrom = getNodeFrom(x, y);
                    isDrawing = true;
                    break;
                case OperationType.ADD_DOMAIN:
                    domainFrom = new Point(x, y);
                    isDrawing = true;
                    break;
                case OperationType.MOVE_NODE:
                    nodeFrom = getNodeFrom(x, y);
                    if (nodeFrom != null)
                    {
                        isDrawing = true;
                        selectAffectedElements(nodeFrom);
                    }
                    System.Threading.Thread.Sleep(10);
                    containerPictureBox.Refresh();
                    break;
            }
        }

        private void containerPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            int x = e.X;
            int y = e.Y;
            putToGrid(ref x, ref y);
            isDrawing = false;

            switch (oType)
            {
                case OperationType.ADD_CONNECTION:
                    if (nodeFrom == null)
                        break;
                    Node nodeTo = getNodeFrom(x, y);
                    if (checkBox1.Checked)
                    {
                        //if(portF > 4)
                        management.addConnection(nodeFrom, management.getPort(nodeFrom), virtualNodeTo, management.getPort(virtualNodeTo));
                        hidePortSetup();
                        containerPictureBox.Refresh();
                    }
                    else
                        showPortSetup(nodeFrom, virtualNodeTo);

                    
                    nodeFrom = null;
                    break;

                case OperationType.ADD_DOMAIN:
                    Point domainTo = new Point(x,y);
                    if (domainFrom.X > x && domainFrom.Y < y)
                    {
                        Point tmpFrom = new Point(domainTo.X, domainFrom.Y);
                        Point tmpTo = new Point(domainFrom.X, domainTo.Y);
                        domainFrom = tmpFrom;
                        domainTo = tmpTo;
                    }
                    else if (domainFrom.X > x && domainFrom.Y > y)
                    {
                        domainTo = domainFrom;
                        domainFrom = new Point(x, y);
                    }
                    else if (domainFrom.X < x && domainFrom.Y > y)
                    {
                        Point tmpFrom = new Point(domainFrom.X, domainTo.Y);
                        Point tmpTo = new Point(domainTo.X, domainFrom.Y);
                        domainFrom = tmpFrom;
                        domainTo = tmpTo;
                    }

                    Domain toAdd = new Domain(domainFrom, domainTo);
                    addDomainToElements(toAdd);
                    break;

                case OperationType.MOVE_NODE:

                    if (nodeFrom == null)
                        break;

                    if (x > containerPictureBox.Size.Width)
                    {
                        x = containerPictureBox.Size.Width;
                        if (y > containerPictureBox.Size.Height)
                            y = containerPictureBox.Size.Height;
                        else if (y < 0)
                            y = 0;
                    }
                    else if (x < 0)
                    {
                        x = 0;
                        if (y > containerPictureBox.Size.Height)
                            y = containerPictureBox.Size.Height;
                        else if (y < 0)
                            y = 0;
                    }

                    Point oldPosition = new Point(nodeFrom.Position.X, nodeFrom.Position.Y);
                    management.isSpaceAvailable(nodeFrom, x, y, containerPictureBox.Size.Height, containerPictureBox.Size.Width);
                    foreach (var elem in connectionTemp)
                        if (elem.Start.Equals(oldPosition))
                            management.addConnection(getNodeFrom(elem.End.X, elem.End.Y), elem.VirtualPortFrom, nodeFrom, elem.VirtualPortTo);
                        else if (elem.End.Equals(oldPosition))
                            management.addConnection(getNodeFrom(elem.Start.X, elem.Start.Y), elem.VirtualPortTo, nodeFrom, elem.VirtualPortFrom);

                    consoleTextBox.AppendText("Node moved from: " + oldPosition.X + "," + oldPosition.Y + " to:" +
                        x + "," + y);
                    consoleTextBox.AppendText(Environment.NewLine);
                    nodeFrom = null;
                    connectionTemp.Clear();
                    break;

            }
            containerPictureBox.Refresh();
        }

        private void containerPictureBox_MouseMove(object sender, MouseEventArgs e)
        {

            if (isDrawing && nodeFrom != null && oType == OperationType.ADD_CONNECTION)
            {
                containerPictureBox.Refresh();
                Point fromNode = new Point(nodeFrom.Position.X, nodeFrom.Position.Y);
                Point to = new Point(e.X, e.Y);

                double distance = Double.PositiveInfinity;
                double temporartDistance = Double.PositiveInfinity;

                foreach (var node in nodeList)
                {
                    temporartDistance = Math.Round(Math.Sqrt(Math.Pow(node.Position.X - e.X, 2) + Math.Pow(node.Position.Y - e.Y, 2)), 2);
                    if (temporartDistance < distance && !node.Equals(nodeFrom))
                    {
                        distance = temporartDistance;
                        virtualNodeTo = node;
                    }
                    temporartDistance = Double.PositiveInfinity;
                }

                Pen blackPen = new Pen(Color.WhiteSmoke, 3);
                if (distance > 100)
                {
                    myGraphics.DrawLine(blackPen, fromNode, to);
                    virtualNodeTo = null;
                }
                else
                {
                    Point end = new Point(virtualNodeTo.Position.X, virtualNodeTo.Position.Y);
                    myGraphics.DrawLine(blackPen, fromNode, end);
                }
                System.Threading.Thread.Sleep(10);
            }
            else if (isDrawing && oType == OperationType.ADD_DOMAIN)
            {
                containerPictureBox.Refresh();

                if (e.X - domainFrom.X < 0 && e.Y - domainFrom.Y < 0)
                {
                    myGraphics.DrawRectangle(new Pen(Color.PaleVioletRed, 3), e.X,
                        e.Y, domainFrom.X - e.X, domainFrom.Y - e.Y);
                }
                else if (e.X - domainFrom.X < 0)
                {
                    myGraphics.DrawRectangle(new Pen(Color.PaleVioletRed, 3), e.X,
                        domainFrom.Y, domainFrom.X - e.X, e.Y - domainFrom.Y);
                }
                else if (e.Y - domainFrom.Y < 0)
                {
                    myGraphics.DrawRectangle(new Pen(Color.PaleVioletRed, 3), domainFrom.X,
                        e.Y, e.X - domainFrom.X, domainFrom.Y - e.Y);
                }
                else
                {
                    myGraphics.DrawRectangle(new Pen(Color.PaleVioletRed, 3), domainFrom.X,
                        domainFrom.Y, e.X - domainFrom.X, e.Y - domainFrom.Y);
                }

                System.Threading.Thread.Sleep(10);
            }
            else if (isDrawing && nodeFrom != null && oType == OperationType.MOVE_NODE)
            {
                containerPictureBox.Refresh();

                Rectangle rect = new Rectangle(e.X - 5, e.Y - 5, 11, 11);
                if (nodeFrom is NetNode)
                    myGraphics.FillEllipse(Brushes.DodgerBlue, rect);
                else if (nodeFrom is ClientNode)
                    myGraphics.FillEllipse(Brushes.YellowGreen, rect);
                myGraphics.DrawEllipse(Pens.Black, rect);
                myGraphics.DrawString(nodeFrom.Name, new Font("Arial", 5), Brushes.Gainsboro, new Point(e.X + 3,
                    e.Y + 3));

                foreach (var elem in connectionTemp)
                {
                    if (elem.Start.Equals(nodeFrom.Position))
                    {
                        drawMovingConnection(myGraphics, elem, new Point(e.X, e.Y));
                    }

                    if (elem.End.Equals(nodeFrom.Position))
                    {
                        drawMovingConnection(myGraphics, elem, new Point(e.X, e.Y));
                    }

                }
                System.Threading.Thread.Sleep(10);
            }
        }

        private void deleteListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            bool isNode = true;

            int idxOfElement = nodeList.IndexOf(nodeList.Where(i => i.Name.Equals(deleteListBox.SelectedItem)).FirstOrDefault());

            if (idxOfElement == -1)
            {
                idxOfElement = connectionList.IndexOf(connectionList.Where(i => i.Name.Equals(deleteListBox.SelectedItem)
                           ).FirstOrDefault());
                isNode = false;
            }

            if (idxOfElement != -1)
            {
                if (isNode)
                {
                    List<NodeConnection> connectionsToDelete = connectionList.Where(
                        i => i.Start.Equals(nodeList.ElementAt(idxOfElement).Position) ||
                        i.End.Equals(nodeList.ElementAt(idxOfElement).Position)
                        ).ToList();

                    foreach (NodeConnection con in connectionsToDelete)
                    {
                        cableHandler.deleteConnection(con);
                        management.removeConnection(con);
                    }
                        

                    management.deleteNode(nodeList.ElementAt(idxOfElement));
                }
                else
                {
                    cableHandler.deleteConnection(connectionList.ElementAt(idxOfElement));
                    management.removeConnection(connectionList.ElementAt(idxOfElement));
                }
                    

            }
            if(deleteListBox.Text.Contains("Restart"))
            {
                management.restartNode(deleteListBox.Text.Split(' ')[1]);
            }

            deleteListBox.Visible = false;
            deleteListBox.Enabled = false;
            deleteListBox.Items.Clear();
            containerPictureBox.Refresh();
        }
        // Auto wigth adding to listbox
        private void autofit()
        {
            int width = deleteListBox.Width;
            using (Graphics g = deleteListBox.CreateGraphics())
            {
                for (int i1 = 0; i1 < deleteListBox.Items.Count; i1++)
                {
                    int itemWidth = Convert.ToInt32(g.MeasureString(Convert.ToString(deleteListBox.Items[i1]), deleteListBox.Font).Width);
                    width = Math.Max(width, itemWidth);
                }
            }
            deleteListBox.Width = width;
        }

        private void clientNodeBtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
            oType = OperationType.ADD_CLIENT_NODE;
        }
        private void networkNodeBtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
            oType = OperationType.ADD_NETWORK_NODE;
        }

        internal void updateConnections(List<NodeConnection> connectionList)
        {
            cableHandler.updateConnections(connectionList);
        }

        private void connectionBtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Cross;
            oType = OperationType.ADD_CONNECTION;
        }
        private void domainBtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Cross;
            oType = OperationType.ADD_DOMAIN;
        }
        private void deleteBtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
            oType = OperationType.DELETE;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
            oType = OperationType.MOVE_NODE;
        }

        public void addNode(Node node)
        {
            if (node is ClientNode)
                consoleTextBox.AppendText("Client Node added at: " + node.Position.X + "," + node.Position.Y + " with adress: " + node.Name);

            if (node is NetNode)
                consoleTextBox.AppendText("Network Node added at: " + node.Position.X + "," + node.Position.Y);

            consoleTextBox.AppendText(Environment.NewLine);

            refreshTable();
        }

        public void errorMessage(String ms)
        {
            consoleTextBox.AppendText(ms);
            consoleTextBox.AppendText(Environment.NewLine);
        }

        private Node getNodeFrom(int x, int y)
        {
            Node n = nodeList.Where(i => i.Position.Equals(new Point(x, y))).FirstOrDefault();
            return n;
        }

        private List<NodeConnection> findConnectionsByPosition(int x, int y)
        {
            List<NodeConnection> result = new List<NodeConnection>();
            NodeConnection ifExist = connectionList.FirstOrDefault(
                i => (i.Start.Equals(new Point(x,y))) || (i.End.Equals(new Point(x,y))));
            if (ifExist != null)
                result = connectionList.AsParallel().Where(
                    i => (i.Start.Equals(new Point(x, y))) || (i.End.Equals(new Point(x, y)))
                    ).ToList();

            return result;
        }
        
        public void bind()
        {
            cableHandler.updateOneConnection();
            consoleTextBox.AppendText("Connection  added");
            consoleTextBox.AppendText(Environment.NewLine);
        }

        public void bind(NodeConnection newNodeConn)
        {
            connectionList.Add(newNodeConn);
            bind();
        }

        private void addDomainToElements(Domain toAdd)
        {
            bool add = true;
            foreach (Domain d in domainList)
            {
                if (toAdd.crossingOtherDomain(d))
                {
                    add = false;
                    break;
                }
            }
            if (toAdd.Size.Width < GAP || toAdd.Size.Height < GAP)
                add = false;
            if (add)
            {
                domainList.Add(toAdd);
                consoleTextBox.AppendText("Domain added");
                consoleTextBox.AppendText(Environment.NewLine);
            }
            else
            {
                consoleTextBox.AppendText("Domains can't cross each others or domain too small for rendering.");
                consoleTextBox.AppendText(Environment.NewLine);
            }
        }

        private void selectAffectedElements(Node node)
        {
            int idxOfElement = nodeList.IndexOf(nodeList.Where(i => i.Name.Equals(node.Name)).FirstOrDefault());

            List<String> atPosition = findConnectionsByPosition(node.Position.X, node.Position.Y).Select(i => i.Name).ToList();
            foreach (String toMove in atPosition)
            {
                idxOfElement = connectionList.IndexOf(connectionList.Where(i => i.Name.Equals(toMove)).FirstOrDefault());
                Console.WriteLine(toMove);
                if (idxOfElement != -1)
                {
                    connectionTemp.Add(connectionList.Where(i => i.Name.Equals(toMove)).FirstOrDefault());
                    connectionList.RemoveAt(idxOfElement);
                }
            }
        }

        private void drawNode(Node node, Graphics panel)
        {
            panel.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(node.Position.X - GAP / 2, node.Position.Y - GAP / 2, GAP + 1, GAP + 1);
            if (node is NetNode)
                panel.FillEllipse(Brushes.DodgerBlue, rect);
            else if (node is ClientNode)
                panel.FillEllipse(Brushes.YellowGreen, rect);
            panel.DrawEllipse(Pens.Black, rect);
            panel.DrawString(node.Name + ":" + node.LocalPort, new Font("Arial", GAP / 2), Brushes.LightGray, new Point(node.Position.X + (GAP / 2),
                node.Position.Y + 3));
        }

        private void drawMovingConnection(Graphics panel, NodeConnection elem, Point end)
        {
            panel.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Point from = elem.Start.Equals(nodeFrom.Position) ?
                            elem.End : elem.Start;
            panel.DrawLine(new Pen(Color.WhiteSmoke, 2), from, end);
            panel.DrawString(elem.Name, new Font("Arial", GAP / 2), Brushes.Gainsboro, new Point((from.X + end.X) / 2 + 3,
               (from.Y + end.Y) / 2 + (GAP / 2)));
        }

        private void drawConnection(NodeConnection conn, Graphics panel)
        {
            panel.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Pen blackPen = new Pen(Color.WhiteSmoke, 2);
            panel.DrawLine(blackPen, conn.Start, conn.End);
            panel.DrawString(conn.Name, new Font("Arial", GAP / 2), Brushes.Gainsboro, new Point((conn.Start.X + conn.End.X) / 2 + (GAP / 2),
               (conn.Start.Y + conn.End.Y) / 2 + (GAP / 2)));
        }

        private void drawElement(Domain domain, Graphics panel)
        {
            panel.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(domain.PointFrom, domain.Size);
            panel.DrawRectangle(new Pen(Color.PaleVioletRed, 3), rect);
        }

        private void drawTrail(Trail t, Graphics panel)
        {
            panel.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Pen orangePen = new Pen(Color.Orange, 3);
            for (int i = 1; i < t.Points.Count(); i++)
            {
                panel.DrawLine(orangePen, t.Points.ElementAt(i - 1), t.Points.ElementAt(i));
            }
            //containerPictureBox.Refresh();
        }

        private void putToGrid(ref int x, ref int y)
        {
            x = GAP * (int)Math.Round((double)x / GAP);
            y = GAP * (int)Math.Round((double)y / GAP);
        }

        private int getNumberOfConnections(Node from, Node to)
        {
            return connectionList.Where(i => (
                        i.Start.Equals(from.Position) &&
                        i.Start.Equals(to.Position)) || (
                        i.Start.Equals(to.Position) &&
                        i.Start.Equals(from.Position))
                        ).Count();
        }

        private void refreshTable()
        {
            var bSource = new BindingSource();
            bSource.DataSource = table;
            dataGridView1.DataSource = bSource;
            dataGridView1.Update();
            dataGridView1.Refresh();
            containerPictureBox.Refresh();
        }

        private void testBtn_Click(object sender, EventArgs e)
        {
            
            //List<List<String>> paths = control.findPaths(nodeList.Where(i => i.Name.Equals("CN0")).FirstOrDefault(), true);
            //if (paths == null)
            //{
            //    consoleTextBox.AppendText("No paths available.");
            //    consoleTextBox.AppendText(Environment.NewLine);
            //}
            //else
            //    foreach (List<String> list in paths)
            //    {
            //        consoleTextBox.AppendText("Path: ");
            //        consoleTextBox.AppendText(Environment.NewLine);
            //        foreach (String str in list)
            //        {
            //            consoleTextBox.AppendText(str);
            //            consoleTextBox.AppendText(Environment.NewLine);
            //        }
            //    }
            management.sendOutInformation();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String portFrom = textBox1.Text;
            String portTo = textBox2.Text;
            if (portFrom.Equals("") || portTo.Equals(""))
                return;
            int portF;
            int portT;
            if (!int.TryParse(portFrom,out portF))
            {
                consoleTextBox.AppendText("Please enter correct ports.");
                consoleTextBox.AppendText(Environment.NewLine);
                return;
            }
            if (!int.TryParse(portTo, out portT))
            {
                consoleTextBox.AppendText("Please enter correct ports in To.");
                consoleTextBox.AppendText(Environment.NewLine);
                return;
            }
            management.addConnection(aNode, portF, bNode, portT);
            hidePortSetup();
            containerPictureBox.Refresh();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            management.stopRunning();
            cableHandler.stopRunning();
        }

        private void saveConfBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save an topology";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                String path = saveFileDialog.InitialDirectory;
                String fileName = saveFileDialog.FileName;
                FileSaver configuration = new FileSaver(path + fileName);
                configuration.WriteToBinaryFile(nodeList, connectionList, domainList,management.TrailList);
            }
        }

        private void readConfBtn_Click(object sender, EventArgs e)
        {
            management.load();
            containerPictureBox.Refresh();
        }

        public void updateLists(List<Node> nodeList, List<Domain> domainList)
        {
            this.nodeList = new List<Node>();
            this.nodeList.AddRange(nodeList);
            this.domainList.AddRange(domainList);
        }

        private void containerPictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int x = e.X;
            int y = e.Y;
            putToGrid(ref x, ref y);
            Node n = getNodeFrom(x, y);
            //SetWindowPos(n.ProcessHandle.MainWindowHandle, 0, 0, 0, 100, 80, 0x2000);
            errorMessage("Paint Trails");
            tempTrailList = new List<Trail>(management.getTrailForNode(n));
            foreach(var a in tempTrailList)
                errorMessage(a.toString());
            //containerPictureBox.Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            management.showTrailWindow();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            management.clearAllTrails();
        }


    }
}
