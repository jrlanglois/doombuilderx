﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Config;

using MoonSharp.Interpreter;

namespace CodeImp.DoomBuilder.DBXLua
{
    [MoonSharpUserData]
    public class Pen
    {
        [MoonSharpHidden]
        public List<PenVertex> drawnVertices;

        public LuaVector2D position;

        public float angle;

        public bool snaptogrid;

        public float stitchrange;
        

        public Pen(LuaVector2D newpos)
        {
            position = newpos;
            drawnVertices = new List<PenVertex>();
            snaptogrid = false;
            stitchrange = 20f;
        }

        public static Pen FromClick()
        {
            return FromClick(ScriptContext.context.snaptogrid, ScriptContext.context.snaptonearest);
        }

        public static Pen FromClick(bool snaptogrid)
        {
            return FromClick(snaptogrid, ScriptContext.context.snaptonearest);
        }

        public static Pen FromClick(bool snaptogrid, bool snaptonearest)
        {
            return new Pen(LuaUI.GetMouseMapPosition(snaptogrid, snaptonearest));
        }

        public static Pen From(float x, float y)
        {
            return new Pen(new LuaVector2D(x,y));
        }

        public static Pen From(LuaVector2D v)
        {
            return new Pen(v);
        }

        public void MoveForward(float distance)
        {
            position.vec += Vector2D.FromAngle(angle, distance);
        }

        public void MoveDiagonal(float dx, float dy)
        {
            MoveDiagonal(new LuaVector2D(dx, dy));
        }

        public void MoveDiagonal(LuaVector2D v)
        {
            position.vec += v.vec.GetRotated(angle);
        }

        public void TurnRight()
        {
            angle -= Angle2D.PIHALF;
        }

        public void TurnLeft()
        {
            angle += Angle2D.PIHALF;
        }

        public void TurnRight(float radians)
        {
            angle -= radians;
        }

        public void TurnLeft(float radians)
        {
            angle += radians;
        }

        public void TurnRightDegrees(float degrees)
        {
            angle -= degrees / Angle2D.PIDEG;
        }

        public void TurnLeftDegrees(float degrees)
        {
            angle += degrees / Angle2D.PIDEG;
        }

        public void SetAngleDegrees(float degrees)
        {
            angle = degrees / Angle2D.PIDEG;
        }

        public void MoveToFirst()
        {
            if (drawnVertices.Count > 0)
            {
                position = drawnVertices[0].pos;
            }
        }

        public void DrawVertex()
        {
            AddVertexAt(position.vec, true, snaptogrid, stitchrange, drawnVertices);
        }

        public void DrawVertex(bool snaptonearest)
        {
            AddVertexAt(position.vec, snaptonearest, snaptogrid, stitchrange, drawnVertices);
        }

        public void DrawVertex(bool snaptonearest, bool bsnaptogrid)
        {
            AddVertexAt(position.vec, snaptonearest, bsnaptogrid, stitchrange, drawnVertices);
        }

        public void DrawVertexAt(LuaVector2D newVec)
        {
            AddVertexAt(newVec.vec, true, snaptogrid, stitchrange, drawnVertices);
        }

        public void DrawVertexAt(LuaVector2D newVec, bool snaptonearest)
        {
            AddVertexAt(newVec.vec, snaptonearest, snaptogrid, stitchrange, drawnVertices);
        }

        public void DrawVertexAt(LuaVector2D newVec, bool snaptonearest, bool snaptogrid)
        {
            AddVertexAt(newVec.vec, snaptonearest, snaptogrid, stitchrange, drawnVertices);
        }

        // FIXME does this actually need a return value?
        public bool FinishPlacingVertices()
        {
            bool b = FinishDrawingPoints(drawnVertices);
            drawnVertices = new List<PenVertex>();
            return b;
        }

        [MoonSharpHidden]
        public bool FinishDrawingPoints(List<PenVertex> points)
        {
            List<DrawnVertex> d = new List<DrawnVertex>(points.Count);

            // convert and also remove consecutive duplicates
            int previndex = -1;
            float vrange = General.Map.FormatInterface.MinLineLength * 0.9f;
            vrange = vrange * vrange;
            foreach (PenVertex p in points)
            {
                DrawnVertex nd = new DrawnVertex();
                nd.pos = p.pos.vec;
                if (!nd.pos.IsFinite())
                {
                    throw new ScriptRuntimeException("Error during finishing pen drawing, position ("
                        + nd.pos
                        + ") with index "
                        + (previndex+1)
                        + " is not a valid vector. (You might have divided by 0 somewhere?)");
                }
                nd.stitch = p.stitch;
                nd.stitchline = p.stitchline;
                if (previndex == -1 || Vector2D.DistanceSq(nd.pos, d[previndex].pos) > 0.001f)
                {
                    d.Add(nd);
                    previndex++;
                }
            }

            // Make the drawing
            if (!Tools.DrawLines(d))
            {
                // ano - i really wish i could give a better error than this, the whole plugin api thing
                // is a bit annoying.
                throw new ScriptRuntimeException("Unknown failure drawing pen vertices.");
            }

            // Snap to map format accuracy
            General.Map.Map.SnapAllToAccuracy();

            // Clear selection
            General.Map.Map.ClearSelectedLinedefs();
            General.Map.Map.ClearSelectedVertices();
            General.Map.Map.ClearSelectedSectors();

            // Update cached values
            General.Map.Map.Update();

            // Update the used textures
            General.Map.Data.UpdateUsedTextures();

            return true;
        }

        // ano - negative stitchrange defaults to builderplug.me.stitchrange which comes from config file
        // most of this is codeimps code
        [MoonSharpHidden]
        internal void AddVertexAt(
            Vector2D mappos, bool snaptonearest, bool snaptogrid, float stitchrange,
            List<PenVertex> points = null)
        {
            PenVertex p = new PenVertex();
            Vector2D vm = mappos;

            float vrange = stitchrange;

            if (vrange <= 0)
            {
                vrange = BuilderPlug.Me.StitchRange;
            }

            // Snap to nearest?
            if (snaptonearest)
            {
                if (points != null)
                {
                    // Go for all drawn points
                    foreach (PenVertex v in points)
                    {
                        if (Vector2D.DistanceSq(mappos, v.pos.vec) < (vrange * vrange))
                        {
                            p.pos = v.pos;
                            p.stitch = true;
                            p.stitchline = true;
                            //Logger.WriteLogLine("a");//debugcrap
                            points.Add(p);
                            return;
                        }
                    }
                    if (points.Count > 0 && Vector2D.DistanceSq(vm, points[points.Count - 1].pos.vec) < 0.001f)
                    {
                        //return points[points.Count - 1];
                        return;
                    }
                }

                // Try the nearest vertex
                Vertex nv = General.Map.Map.NearestVertexSquareRange(mappos, vrange);
                if (nv != null)
                {
                    p.pos.vec = nv.Position;
                    p.stitch = true;
                    p.stitchline = true;
                    //Logger.WriteLogLine("b");//debugcrap
                    points.Add(p);
                    return;
                }

                // Try the nearest linedef
                Linedef nl = General.Map.Map.NearestLinedefRange(mappos, vrange);
                if (nl != null)
                {
                    // Snap to grid?
                    if (snaptogrid)
                    {
                        // Get grid intersection coordinates
                        List<Vector2D> coords = nl.GetGridIntersections();

                        // Find nearest grid intersection
                        bool found = false;
                        float found_distance = float.MaxValue;
                        Vector2D found_coord = new Vector2D();
                        foreach (Vector2D v in coords)
                        {
                            Vector2D delta = mappos - v;
                            if (delta.GetLengthSq() < found_distance)
                            {
                                found_distance = delta.GetLengthSq();
                                found_coord = v;
                                found = true;
                            }
                        }

                        if (found)
                        {
                            // Align to the closest grid intersection
                            p.pos.vec = found_coord;
                            p.stitch = true;
                            p.stitchline = true;
                            //Logger.WriteLogLine("c");//debugcrap
                            points.Add(p);
                            return;
                        }
                    }
                    else
                    {
                        // Aligned to line
                        p.pos.vec = nl.NearestOnLine(mappos);
                        p.stitch = true;
                        p.stitchline = true;
                        //Logger.WriteLogLine("d");//debugcrap
                        points.Add(p);
                        return;
                    }
                }
            }
            else
            {
                // Always snap to the first drawn vertex so that the user can finish a complete sector without stitching
                if (points != null && points.Count > 0)
                {
                    if (Vector2D.DistanceSq(mappos, points[0].pos.vec) < (vrange * vrange))
                    {
                        p.pos = points[0].pos;
                        p.stitch = true;
                        p.stitchline = false;
                        //Logger.WriteLogLine("e");//debugcrap
                        points.Add(p);
                        return;
                    }
                }
            }

            // if the mouse cursor is outside the map bondaries check if the line between the last set point and the
            // mouse cursor intersect any of the boundary lines. If it does, set the position to this intersection
            if (points != null &&
                points.Count > 0 &&
                (mappos.x < General.Map.Config.LeftBoundary || mappos.x > General.Map.Config.RightBoundary ||
                mappos.y > General.Map.Config.TopBoundary || mappos.y < General.Map.Config.BottomBoundary))
            {
                Line2D dline = new Line2D(mappos, points[points.Count - 1].pos.vec);
                bool foundintersection = false;
                float u = 0.0f;
                List<Line2D> blines = new List<Line2D>();

                // lines for left, top, right and bottom bondaries
                blines.Add(new Line2D(General.Map.Config.LeftBoundary, General.Map.Config.BottomBoundary, General.Map.Config.LeftBoundary, General.Map.Config.TopBoundary));
                blines.Add(new Line2D(General.Map.Config.LeftBoundary, General.Map.Config.TopBoundary, General.Map.Config.RightBoundary, General.Map.Config.TopBoundary));
                blines.Add(new Line2D(General.Map.Config.RightBoundary, General.Map.Config.TopBoundary, General.Map.Config.RightBoundary, General.Map.Config.BottomBoundary));
                blines.Add(new Line2D(General.Map.Config.RightBoundary, General.Map.Config.BottomBoundary, General.Map.Config.LeftBoundary, General.Map.Config.BottomBoundary));

                // check for intersections with boundaries
                for (int i = 0; i < blines.Count; i++)
                {
                    if (!foundintersection)
                    {
                        // only check for intersection if the last set point is not on the
                        // line we are checking against
                        if (blines[i].GetSideOfLine(points[points.Count - 1].pos.vec) != 0.0f)
                        {
                            foundintersection = blines[i].GetIntersection(dline, out u);
                        }
                    }
                }

                // if there was no intersection set the position to the last set point
                if (!foundintersection)
                    vm = points[points.Count - 1].pos.vec;
                else
                    vm = dline.GetCoordinatesAt(u);

            }


            // Snap to grid?
            if (snaptogrid)
            {
                // Aligned to grid
                p.pos.vec = General.Map.Grid.SnappedToGrid(vm);

                // special handling 
                if (p.pos.vec.x > General.Map.Config.RightBoundary) p.pos.vec.x = General.Map.Config.RightBoundary;
                if (p.pos.vec.y < General.Map.Config.BottomBoundary) p.pos.vec.y = General.Map.Config.BottomBoundary;
                p.stitch = snaptonearest;
                p.stitchline = snaptonearest;
                //Logger.WriteLogLine("f");//debugcrap
                points.Add(p);
                return;
            }
            else
            {
                // Normal position
                p.pos.vec = vm;
                p.stitch = snaptonearest;
                p.stitchline = snaptonearest;
                //Logger.WriteLogLine("g");//debugcrap
                points.Add(p);
                return;
            }
        } // getcurrentposition

    } // pen
} // ns
