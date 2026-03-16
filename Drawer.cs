using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KiCad2Gcode
{
    internal class Drawer
    {
        PictureBox pBox;
        Panel pPanel;
        Bitmap bmp;

        bool drawDots = false;

        float H = 600;

        int scale = 10;
        int offX = 0;
        int offY = 0;

        public Drawer(PictureBox pBox_, Panel pPanel)
        {
            this.pBox = pBox_;
            this.pPanel = pPanel;
        }

        public void SetScale(int scale_)
        {
            scale = scale_;
        }

        public void SetPos(int x, int y)
        {
            offX = x;
            offY = y;
        }

        private void DrawDotInt(Point2D position, int size, Bitmap bmp, System.Drawing.Color color)
        {
            if (position.type == Point2D.PointType_et.CROSS_X)
            {
                color = Color.Red;
            }
            else if (position.type == Point2D.PointType_et.CROSS_T)
            {
                color = Color.Purple;
            }

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillEllipse(
                    new SolidBrush(color),
                    (float)(position.x * scale + offX - size),
                    H - (float)(position.y * scale + offY + size),
                    (float)(size * 2),
                    (float)(size * 2));
            }
        }

        private void DrawCircleInt(Point2D position, int size, Bitmap bmp, System.Drawing.Color color)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawEllipse(
                    new Pen(color),
                    (float)(position.x * scale + offX - size),
                    H - (float)(position.y * scale + offY + size),
                    (float)(size * 2),
                    (float)(size * 2));
            }
        }

        public void DrawDot(Point2D position, int size, System.Drawing.Color color)
        {
            if (bmp == null) return;

            DrawDotInt(position, size, bmp, color);
            pBox.Image = bmp;
            pBox.Refresh();
        }

        public void DrawElement(Point2D startPt, Point2D endPt, Arc arc)
        {
            if (bmp == null) return;

            DrawChunk(startPt, endPt, arc, bmp, System.Drawing.Color.Black);
            pBox.Image = bmp;
            pBox.Refresh();
        }

        public void DrawListOfElements(LinkedList<Node> list)
        {
            if (bmp == null || list == null) return;

            LinkedListNode<Node> n = list.First;

            while (n != null)
            {
                Point2D startPt;
                if (n.Value.startPt != null)
                {
                    startPt = n.Value.startPt;
                }
                else
                {
                    LinkedListNode<Node> prevNode = n.Previous ?? n.List.Last;
                    startPt = prevNode.Value.pt;
                }

                System.Drawing.Color color = System.Drawing.Color.Black;

                if (n.Value.active)
                {
                    color = System.Drawing.Color.Red;
                }

                DrawChunk(startPt, n.Value.pt, n.Value.arc, bmp, color);
                n = n.Next;
            }

            pBox.Image = bmp;
            pBox.Refresh();
        }

        public void SetCentre(Point2D position)
        {
            pPanel.AutoScrollPosition = new Point(
                (int)(scale * position.x) - 1000,
                (int)H - (int)(scale * position.y) - 600);
        }

        private void DrawChunk(Point2D startPt, Point2D endPt, Arc arc, Bitmap bmp, System.Drawing.Color color)
        {
            if (arc == null)
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawLine(
                        new Pen(color),
                        (float)startPt.x * scale + offX,
                        H - ((float)startPt.y * scale + offY),
                        (float)endPt.x * scale + offX,
                        H - ((float)endPt.y * scale + offY));
                }
            }
            else
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    float angleStart;
                    float angleEnd;

                    if (arc.ccw == false)
                    {
                        angleStart = (float)(arc.startAngle * 180 / Math.PI);
                        angleEnd = (float)(arc.endAngle * 180 / Math.PI);
                    }
                    else
                    {
                        angleEnd = (float)(arc.startAngle * 180 / Math.PI);
                        angleStart = (float)(arc.endAngle * 180 / Math.PI);
                    }

                    float angleSweep = angleStart - angleEnd;
                    while (angleSweep < 0) { angleSweep += 360; }
                    while (angleSweep > 360) { angleSweep -= 360; }

                    angleStart = -angleStart;
                    while (angleStart < 0) { angleStart += 360; }
                    while (angleStart > 360) { angleStart -= 360; }

                    g.DrawArc(
                        new Pen(color),
                        (float)((arc.centre.x - arc.radius) * scale + offX),
                        H - ((float)((arc.centre.y + arc.radius) * scale + offY)),
                        (float)(arc.radius * 2 * scale),
                        (float)(arc.radius * 2 * scale),
                        angleStart,
                        angleSweep);
                }
            }
        }

        private void DrawDrill(Drill drill, Bitmap bmp)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawArc(
                    new Pen(Color.Black),
                    (float)((drill.pos.x - drill.diameter / 2) * scale + offX),
                    H - ((float)((drill.pos.y + drill.diameter / 2) * scale + offY)),
                    (float)(drill.diameter * scale),
                    (float)(drill.diameter * scale),
                    0,
                    360);
            }
        }

        public void InitBitmap(int xSize, int ySize)
        {
            bmp = new Bitmap(xSize, ySize);
            pBox.Width = bmp.Width;
            pBox.Height = bmp.Height;

            offX = 0;
            offY = 0;

            H = ySize;
            scale = 1;

            pBox.Image = bmp;
            pBox.Refresh();
        }

        public void DrawNetlist(Net[] netList)
        {
            if (bmp == null) return;

            if (netList != null)
            {
                foreach (Net net in netList)
                {
                    foreach (Figure f in net.figures)
                    {
                        LinkedListNode<Node> n = f.shape.points.First;
                        bool first = true;

                        while (n != null)
                        {
                            LinkedListNode<Node> nPrev = n.Previous ?? f.shape.points.Last;

                            Color color = Color.Red;

                            if (n.Value.active == true)
                            {
                                color = Color.Black;
                            }
                            else if (f.shape.selected == 1)
                            {
                                color = Color.Green;
                            }
                            else if (f.shape.selected == 2)
                            {
                                color = Color.LightBlue;
                            }
                            else if (n.Value.pt.type == Point2D.PointType_et.BRIDGE)
                            {
                                color = Color.Cyan;
                            }

                            DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, color);

                            if (drawDots)
                            {
                                DrawDotInt(n.Value.pt, 2, bmp, first ? Color.DarkOrange : Color.Black);
                            }

                            if (first)
                            {
                                first = false;
                            }

                            n = n.Next;
                        }

                        foreach (Polygon p in f.holes)
                        {
                            n = p.points.First;
                            first = true;

                            while (n != null)
                            {
                                LinkedListNode<Node> nPrev = n.Previous ?? p.points.Last;

                                Color color = Color.Blue;
                                if (p.selected == 1)
                                {
                                    color = Color.Green;
                                }
                                else if (p.selected == 2)
                                {
                                    color = Color.LightBlue;
                                }
                                else if (p.selected == 3)
                                {
                                    color = Color.Pink;
                                }

                                DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, color);

                                if (drawDots)
                                {
                                    DrawDotInt(n.Value.pt, 3, bmp, first ? Color.DarkOrange : Color.Black);
                                }

                                if (first)
                                {
                                    first = false;
                                }

                                n = n.Next;
                            }
                        }
                    }

                    foreach (Figure f in net.zoneFigures)
                    {
                        LinkedListNode<Node> n = f.shape.points.First;
                        bool first = true;

                        while (n != null)
                        {
                            LinkedListNode<Node> nPrev = n.Previous ?? f.shape.points.Last;

                            Color color = Color.Red;
                            if (f.shape.selected == 1)
                            {
                                color = Color.Green;
                            }
                            else if (f.shape.selected == 2)
                            {
                                color = Color.LightBlue;
                            }
                            else if (n.Value.pt.type == Point2D.PointType_et.BRIDGE)
                            {
                                color = Color.Cyan;
                            }

                            DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, color);

                            if (drawDots)
                            {
                                DrawDotInt(n.Value.pt, 3, bmp, first ? Color.DarkOrange : Color.Black);
                            }

                            if (first)
                            {
                                first = false;
                            }

                            n = n.Next;
                        }

                        foreach (Polygon p in f.holes)
                        {
                            n = p.points.First;
                            first = true;

                            while (n != null)
                            {
                                LinkedListNode<Node> nPrev = n.Previous ?? p.points.Last;

                                Color color = Color.Blue;
                                if (p.selected == 1)
                                {
                                    color = Color.Green;
                                }
                                else if (p.selected == 2)
                                {
                                    color = Color.LightBlue;
                                }

                                DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, color);

                                if (drawDots)
                                {
                                    DrawDotInt(n.Value.pt, first ? 4 : 2, bmp, first ? Color.DarkOrange : Color.Black);
                                }

                                if (first)
                                {
                                    first = false;
                                }

                                n = n.Next;
                            }
                        }
                    }
                }
            }

            pBox.Refresh();
        }

        public void DrawCuts(Figure cuts)
        {
            if (bmp == null) return;

            if (cuts != null)
            {
                if (cuts.shape != null)
                {
                    LinkedListNode<Node> nc = cuts.shape.points.First;

                    while (nc != null)
                    {
                        LinkedListNode<Node> nPrev = nc.Previous ?? cuts.shape.points.Last;
                        DrawChunk(nPrev.Value.pt, nc.Value.pt, nc.Value.arc, bmp, Color.Black);
                        nc = nc.Next;
                    }
                }

                foreach (Polygon p in cuts.holes)
                {
                    LinkedListNode<Node> nc = p.points.First;

                    while (nc != null)
                    {
                        LinkedListNode<Node> nPrev = nc.Previous ?? p.points.Last;
                        DrawChunk(nPrev.Value.pt, nc.Value.pt, nc.Value.arc, bmp, Color.Violet);
                        nc = nc.Next;
                    }
                }
            }

            pBox.Refresh();
        }

        public void DrawDrills(List<Drill> drills)
        {
            if (bmp == null || drills == null) return;

            foreach (Drill drill in drills)
            {
                DrawDrill(drill, bmp);
            }

            pBox.Refresh();
        }

        public void DrawMillPath(List<Polygon> millPath)
        {
            if (bmp == null || millPath == null) return;

            foreach (Polygon p in millPath)
            {
                LinkedListNode<Node> n = p.points.First;
                bool first = true;

                while (n != null)
                {
                    LinkedListNode<Node> nPrev = n.Previous ?? p.points.Last;
                    DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, Color.LightBlue);

                    if (drawDots)
                    {
                        if (first)
                        {
                            first = false;
                            DrawCircleInt(n.Value.pt, 3, bmp, Color.DarkViolet);
                        }

                        if (n.Value.pt.type == Point2D.PointType_et.CROSS_X)
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.LightBlue);
                        }
                        else if (n.Value.pt.state == Point2D.STATE_et.BAD)
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.OrangeRed);
                        }
                        else
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.Black);
                        }
                    }

                    n = n.Next;
                }

                pBox.Image = bmp;
                pBox.Refresh();
            }

            pBox.Refresh();
        }

        public void DrawBoardMillPath(Polygon boardMillPath)
        {
            if (bmp == null) return;

            if (boardMillPath != null)
            {
                LinkedListNode<Node> n = boardMillPath.points.First;
                bool first = true;

                while (n != null)
                {
                    LinkedListNode<Node> nPrev = n.Previous ?? boardMillPath.points.Last;
                    DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, Color.LightBlue);

                    if (drawDots)
                    {
                        if (first)
                        {
                            first = false;
                            DrawCircleInt(n.Value.pt, 3, bmp, Color.DarkViolet);
                        }

                        if (n.Value.pt.type == Point2D.PointType_et.CROSS_X)
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.LightBlue);
                        }
                        else if (n.Value.pt.state == Point2D.STATE_et.BAD)
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.OrangeRed);
                        }
                        else
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.Black);
                        }
                    }

                    n = n.Next;
                }
            }

            pBox.Refresh();
        }

        public void DrawBoardHolesMillPath(List<Polygon> boardHolesMillPath)
        {
            if (bmp == null || boardHolesMillPath == null) return;

            foreach (Polygon p in boardHolesMillPath)
            {
                LinkedListNode<Node> n = p.points.First;
                bool first = true;

                while (n != null)
                {
                    LinkedListNode<Node> nPrev = n.Previous ?? p.points.Last;
                    DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, Color.LightBlue);

                    if (drawDots)
                    {
                        if (first)
                        {
                            first = false;
                            DrawCircleInt(n.Value.pt, 3, bmp, Color.DarkViolet);
                        }

                        if (n.Value.pt.type == Point2D.PointType_et.CROSS_X)
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.LightBlue);
                        }
                        else if (n.Value.pt.state == Point2D.STATE_et.BAD)
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.OrangeRed);
                        }
                        else
                        {
                            DrawDotInt(n.Value.pt, 2, bmp, Color.Black);
                        }
                    }

                    n = n.Next;
                }
            }

            pBox.Refresh();
        }

        public void DrawFieldMillPath(Polygon p)
        {
            if (bmp == null || p == null) return;

            LinkedListNode<Node> n = p.points.First;
            bool first = true;

            while (n != null)
            {
                LinkedListNode<Node> nPrev = n.Previous ?? p.points.Last;
                DrawChunk(nPrev.Value.pt, n.Value.pt, n.Value.arc, bmp, Color.LightGreen);

                if (drawDots)
                {
                    if (first)
                    {
                        first = false;
                        DrawCircleInt(n.Value.pt, 3, bmp, Color.DarkViolet);
                    }

                    if (n.Value.pt.type == Point2D.PointType_et.CROSS_X)
                    {
                        DrawDotInt(n.Value.pt, 2, bmp, Color.LightBlue);
                    }
                    else if (n.Value.pt.state == Point2D.STATE_et.BAD)
                    {
                        DrawDotInt(n.Value.pt, 2, bmp, Color.OrangeRed);
                    }
                    else
                    {
                        DrawDotInt(n.Value.pt, 2, bmp, Color.Black);
                    }
                }

                n = n.Next;
            }

            pBox.Refresh();
        }

        public void DrawFieldMillPathList(List<Polygon> fieldsMillPath)
        {
            if (bmp == null || fieldsMillPath == null) return;

            foreach (Polygon p in fieldsMillPath)
            {
                DrawFieldMillPath(p);
            }
        }

        public void InitDrawer(Net[] netList, Figure cuts)
        {
            double minX = 0, maxX = 0, minY = 0, maxY = 0;
            bool valid = false;

            if (netList != null)
            {
                foreach (Net net in netList)
                {
                    if (net.figures != null)
                    {
                        foreach (Figure f in net.figures)
                        {
                            if (f == null || f.shape == null || f.shape.extPoint == null || f.shape.extPoint.Length < 4)
                            {
                                continue;
                            }

                            if (valid == false)
                            {
                                minX = f.shape.extPoint[0].x;
                                maxY = f.shape.extPoint[1].y;
                                maxX = f.shape.extPoint[2].x;
                                minY = f.shape.extPoint[3].y;
                                valid = true;
                            }

                            if (f.shape.extPoint[0].x < minX) { minX = f.shape.extPoint[0].x; }
                            if (f.shape.extPoint[1].y > maxY) { maxY = f.shape.extPoint[1].y; }
                            if (f.shape.extPoint[2].x > maxX) { maxX = f.shape.extPoint[2].x; }
                            if (f.shape.extPoint[3].y < minY) { minY = f.shape.extPoint[3].y; }
                        }
                    }
                }

                foreach (Net net in netList)
                {
                    if (net.zoneFigures != null)
                    {
                        foreach (Figure f in net.zoneFigures)
                        {
                            if (f == null || f.shape == null || f.shape.extPoint == null || f.shape.extPoint.Length < 4)
                            {
                                continue;
                            }

                            if (valid == false)
                            {
                                minX = f.shape.extPoint[0].x;
                                maxY = f.shape.extPoint[1].y;
                                maxX = f.shape.extPoint[2].x;
                                minY = f.shape.extPoint[3].y;
                                valid = true;
                            }

                            if (f.shape.extPoint[0].x < minX) { minX = f.shape.extPoint[0].x; }
                            if (f.shape.extPoint[1].y > maxY) { maxY = f.shape.extPoint[1].y; }
                            if (f.shape.extPoint[2].x > maxX) { maxX = f.shape.extPoint[2].x; }
                            if (f.shape.extPoint[3].y < minY) { minY = f.shape.extPoint[3].y; }
                        }
                    }
                }
            }

            if (cuts != null &&
                cuts.shape != null &&
                cuts.shape.points != null &&
                cuts.shape.points.Count > 0 &&
                cuts.shape.extPoint != null &&
                cuts.shape.extPoint.Length >= 4)
            {
                if (valid == false)
                {
                    minX = cuts.shape.extPoint[0].x;
                    maxY = cuts.shape.extPoint[1].y;
                    maxX = cuts.shape.extPoint[2].x;
                    minY = cuts.shape.extPoint[3].y;
                    valid = true;
                }
                else
                {
                    if (cuts.shape.extPoint[0].x < minX) { minX = cuts.shape.extPoint[0].x; }
                    if (cuts.shape.extPoint[1].y > maxY) { maxY = cuts.shape.extPoint[1].y; }
                    if (cuts.shape.extPoint[2].x > maxX) { maxX = cuts.shape.extPoint[2].x; }
                    if (cuts.shape.extPoint[3].y < minY) { minY = cuts.shape.extPoint[3].y; }
                }
            }

            if (valid == false)
            {
                bmp = null;
                pBox.Image = null;
                return;
            }

            maxX += 10;
            minX -= 10;
            maxY += 10;
            minY -= 10;

            double sizeXd = maxX - minX;
            double sizeYd = maxY - minY;

            int sizeX = (int)(sizeXd * scale);
            int sizeY = (int)(sizeYd * scale);

            if (sizeX <= 0 || sizeY <= 0 || sizeX > 20000 || sizeY > 20000)
            {
                MessageBox.Show(
                    "Invalid bitmap size before creation.\n\n" +
                    "minX = " + minX + "\n" +
                    "maxX = " + maxX + "\n" +
                    "minY = " + minY + "\n" +
                    "maxY = " + maxY + "\n" +
                    "sizeXd = " + sizeXd + "\n" +
                    "sizeYd = " + sizeYd + "\n" +
                    "scale = " + scale + "\n" +
                    "sizeX = " + sizeX + "\n" +
                    "sizeY = " + sizeY,
                    "KiCad2Gcode Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                bmp = null;
                pBox.Image = null;
                return;
            }

            try
            {
                bmp = new Bitmap(sizeX, sizeY);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Bitmap creation failed.\n\n" +
                    "minX = " + minX + "\n" +
                    "maxX = " + maxX + "\n" +
                    "minY = " + minY + "\n" +
                    "maxY = " + maxY + "\n" +
                    "sizeXd = " + sizeXd + "\n" +
                    "sizeYd = " + sizeYd + "\n" +
                    "scale = " + scale + "\n" +
                    "sizeX = " + sizeX + "\n" +
                    "sizeY = " + sizeY + "\n\n" +
                    ex.ToString(),
                    "Bitmap Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                bmp = null;
                pBox.Image = null;
                return;
            }

            pBox.Width = bmp.Width;
            pBox.Height = bmp.Height;

            offX = (int)(-minX * scale);
            offY = (int)(-minY * scale);

            H = sizeY;

            pBox.Image = bmp;
        }

        public void Redraw(Net[] netList, Figure cuts, List<Drill> drills, List<Polygon> millPath, Polygon boardMillPath, List<Polygon> boardHolesMillPath, List<Polygon> fieldsMillPath)
        {
            InitDrawer(netList, cuts);

            if (bmp == null)
            {
                return;
            }

            DrawNetlist(netList);
            DrawCuts(cuts);
            DrawBoardHolesMillPath(boardHolesMillPath);
            DrawDrills(drills);
            DrawMillPath(millPath);
            DrawFieldMillPathList(fieldsMillPath);
            DrawBoardMillPath(boardMillPath);
        }
    }
}