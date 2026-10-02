using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// Minimal SVG path-data parser (M L H V C S Q T A Z, absolute and relative) that appends
    /// to a GraphicsPath. Quadratics are raised to cubics; arcs are converted to cubics.
    /// </summary>
    internal static class SvgPath
    {
        public static void AppendTo(GraphicsPath path, string d)
        {
            Scanner s = new Scanner(d);
            PointF cur = PointF.Empty, start = PointF.Empty;
            PointF lastCtrl = PointF.Empty;   // reflection point for S/T
            char lastCmd = ' ';
            char cmd = ' ';
            bool figureOpen = false;

            while (true)
            {
                s.SkipSeparators();
                if (s.AtEnd) break;
                char c = s.Peek();
                if (char.IsLetter(c)) { cmd = c; s.Next(); }
                else if (cmd == ' ') throw new FormatException("path must start with a command");
                // An implicit repeat of M is L (and m is l).
                else if (cmd == 'M') cmd = 'L';
                else if (cmd == 'm') cmd = 'l';

                bool rel = char.IsLower(cmd);
                char up = char.ToUpperInvariant(cmd);
                PointF origin = rel ? cur : PointF.Empty;

                switch (up)
                {
                    case 'M':
                    {
                        PointF p = Add(origin, s.Number(), s.Number());
                        path.StartFigure();
                        figureOpen = true;
                        cur = start = p;
                        break;
                    }
                    case 'L':
                    {
                        PointF p = Add(origin, s.Number(), s.Number());
                        path.AddLine(cur, p);
                        cur = p;
                        break;
                    }
                    case 'H':
                    {
                        float x = s.Number() + (rel ? cur.X : 0);
                        PointF p = new PointF(x, cur.Y);
                        path.AddLine(cur, p);
                        cur = p;
                        break;
                    }
                    case 'V':
                    {
                        float y = s.Number() + (rel ? cur.Y : 0);
                        PointF p = new PointF(cur.X, y);
                        path.AddLine(cur, p);
                        cur = p;
                        break;
                    }
                    case 'C':
                    {
                        PointF c1 = Add(origin, s.Number(), s.Number());
                        PointF c2 = Add(origin, s.Number(), s.Number());
                        PointF p = Add(origin, s.Number(), s.Number());
                        path.AddBezier(cur, c1, c2, p);
                        lastCtrl = c2; cur = p;
                        break;
                    }
                    case 'S':
                    {
                        char lu = char.ToUpperInvariant(lastCmd);
                        PointF c1 = (lu == 'C' || lu == 'S') ? Reflect(lastCtrl, cur) : cur;
                        PointF c2 = Add(origin, s.Number(), s.Number());
                        PointF p = Add(origin, s.Number(), s.Number());
                        path.AddBezier(cur, c1, c2, p);
                        lastCtrl = c2; cur = p;
                        break;
                    }
                    case 'Q':
                    {
                        PointF q = Add(origin, s.Number(), s.Number());
                        PointF p = Add(origin, s.Number(), s.Number());
                        Quad(path, cur, q, p);
                        lastCtrl = q; cur = p;
                        break;
                    }
                    case 'T':
                    {
                        char lu = char.ToUpperInvariant(lastCmd);
                        PointF q = (lu == 'Q' || lu == 'T') ? Reflect(lastCtrl, cur) : cur;
                        PointF p = Add(origin, s.Number(), s.Number());
                        Quad(path, cur, q, p);
                        lastCtrl = q; cur = p;
                        break;
                    }
                    case 'A':
                    {
                        float rx = s.Number(), ry = s.Number(), rot = s.Number();
                        bool large = s.Flag(), sweep = s.Flag();
                        PointF p = Add(origin, s.Number(), s.Number());
                        Arc(path, cur, p, rx, ry, rot, large, sweep);
                        cur = p;
                        break;
                    }
                    case 'Z':
                    {
                        if (figureOpen) path.CloseFigure();
                        figureOpen = false;
                        cur = start;
                        break;
                    }
                    default:
                        throw new FormatException("unsupported path command " + cmd);
                }
                lastCmd = cmd;
            }
        }

        private static PointF Add(PointF o, float x, float y) { return new PointF(o.X + x, o.Y + y); }
        private static PointF Reflect(PointF ctrl, PointF about) { return new PointF(2 * about.X - ctrl.X, 2 * about.Y - ctrl.Y); }

        private static void Quad(GraphicsPath path, PointF p0, PointF q, PointF p)
        {
            PointF c1 = new PointF(p0.X + 2f / 3f * (q.X - p0.X), p0.Y + 2f / 3f * (q.Y - p0.Y));
            PointF c2 = new PointF(p.X + 2f / 3f * (q.X - p.X), p.Y + 2f / 3f * (q.Y - p.Y));
            path.AddBezier(p0, c1, c2, p);
        }

        /// <summary>SVG endpoint arc -> centre parameterisation -> cubic segments (SVG spec F.6).</summary>
        private static void Arc(GraphicsPath path, PointF p0, PointF p1, float rxIn, float ryIn, float angleDeg, bool large, bool sweep)
        {
            if (p0 == p1) return;
            double rx = Math.Abs(rxIn), ry = Math.Abs(ryIn);
            if (rx < 1e-6 || ry < 1e-6) { path.AddLine(p0, p1); return; }

            double phi = angleDeg * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);
            double dx = (p0.X - p1.X) / 2.0, dy = (p0.Y - p1.Y) / 2.0;
            double x1p = cosPhi * dx + sinPhi * dy;
            double y1p = -sinPhi * dx + cosPhi * dy;

            double lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
            if (lambda > 1) { double sq = Math.Sqrt(lambda); rx *= sq; ry *= sq; }

            double num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
            double den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
            double coef = (den == 0) ? 0 : Math.Sqrt(Math.Max(0, num / den));
            if (large == sweep) coef = -coef;
            double cxp = coef * (rx * y1p / ry);
            double cyp = coef * -(ry * x1p / rx);

            double cx = cosPhi * cxp - sinPhi * cyp + (p0.X + p1.X) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (p0.Y + p1.Y) / 2.0;

            double theta1 = VectorAngle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
            double dTheta = VectorAngle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
            if (!sweep && dTheta > 0) dTheta -= 2 * Math.PI;
            else if (sweep && dTheta < 0) dTheta += 2 * Math.PI;

            int segments = (int)Math.Ceiling(Math.Abs(dTheta) / (Math.PI / 2) - 1e-9);
            if (segments < 1) segments = 1;
            double delta = dTheta / segments;
            double t = 4.0 / 3.0 * Math.Tan(delta / 4);

            PointF from = p0;
            double a = theta1;
            for (int i = 0; i < segments; i++)
            {
                double cosA = Math.Cos(a), sinA = Math.Sin(a);
                double b = a + delta;
                double cosB = Math.Cos(b), sinB = Math.Sin(b);

                PointF c1 = Map(cx, cy, rx, ry, cosPhi, sinPhi, cosA - t * sinA, sinA + t * cosA);
                PointF c2 = Map(cx, cy, rx, ry, cosPhi, sinPhi, cosB + t * sinB, sinB - t * cosB);
                PointF to = (i == segments - 1) ? p1 : Map(cx, cy, rx, ry, cosPhi, sinPhi, cosB, sinB);
                path.AddBezier(from, c1, c2, to);
                from = to;
                a = b;
            }
        }

        private static PointF Map(double cx, double cy, double rx, double ry, double cosPhi, double sinPhi, double ux, double uy)
        {
            double x = rx * ux, y = ry * uy;
            return new PointF((float)(cosPhi * x - sinPhi * y + cx), (float)(sinPhi * x + cosPhi * y + cy));
        }

        private static double VectorAngle(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = Math.Sqrt(ux * ux + uy * uy) * Math.Sqrt(vx * vx + vy * vy);
            double ang = Math.Acos(Math.Max(-1, Math.Min(1, dot / len)));
            if (ux * vy - uy * vx < 0) ang = -ang;
            return ang;
        }

        private sealed class Scanner
        {
            private readonly string _s;
            private int _i;
            public Scanner(string s) { _s = s; }
            public bool AtEnd { get { return _i >= _s.Length; } }
            public char Peek() { return _s[_i]; }
            public void Next() { _i++; }

            public void SkipSeparators()
            {
                while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == ',' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r')) _i++;
            }

            /// <summary>Arc flags may be packed without separators ("a2 2 0 012 2").</summary>
            public bool Flag()
            {
                SkipSeparators();
                if (_i >= _s.Length) throw new FormatException("expected flag");
                char c = _s[_i++];
                if (c != '0' && c != '1') throw new FormatException("bad arc flag '" + c + "'");
                return c == '1';
            }

            /// <summary>SVG numbers: "1.307-.193" is two numbers, ".5.5" is two numbers.</summary>
            public float Number()
            {
                SkipSeparators();
                int begin = _i;
                if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                bool dot = false, digits = false;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c >= '0' && c <= '9') { digits = true; _i++; }
                    else if (c == '.' && !dot) { dot = true; _i++; }
                    else break;
                }
                if (digits && _i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E'))
                {
                    int save = _i;
                    _i++;
                    if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                    if (_i < _s.Length && char.IsDigit(_s[_i])) { while (_i < _s.Length && char.IsDigit(_s[_i])) _i++; }
                    else _i = save;
                }
                if (!digits) throw new FormatException("expected number at " + begin);
                return float.Parse(_s.Substring(begin, _i - begin), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
