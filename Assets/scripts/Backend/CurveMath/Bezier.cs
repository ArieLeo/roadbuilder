﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
///   <para>P0 P1 P2 should not be on the same line</para>
/// </summary>
public class Bezier : Curve
{
    public Vector2 P0
    {
        get;
        private set;
    }
       
    public Vector2 P1
    {
        get;
        private set;
    }

    public Vector2 P2
    {
        get;
        private set;
    }

    // Non-required control point
    public Vector2 P3
    {
        get
        {
            return 0.25f * P0 + 0.5f * P1 + 0.25f * P2;
        }
    }

    public override bool IsValid => 
    !float.IsInfinity(P0.x) && !float.IsInfinity(P2.x) && !Algebra.isclose(P0, P2);

    protected override void Invalidate()
    {
        P0 = Vector2.negativeInfinity;
    }

    public static Curve GetDefault()
    {
        return new Bezier(Vector2.negativeInfinity, Vector2.negativeInfinity, Vector2.negativeInfinity);
    }

    public Bezier(Vector2 _P0, Vector2 _P1, Vector2 _P2)
    {
        P0 = _P0;
        P1 = _P1;
        P2 = _P2;
        t_start = 0f;
        t_end = 1f;
    }

    public override List<Vector2> ControlPoints
    {
        get
        {
            return new List<Vector2> { P0, P1, P3, P2 };
        }
        set
        {
            // At most one value got changed
            if (P0.x != value[0].x)
            {
                P0 = value[0];
            }
            else
            {
                if (P1.x != value[1].x)
                {
                    P1 = value[1];
                }
                else
                {
                    if (P2.x != value[3].x)
                    {
                        P2 = value[3];
                    }
                    else
                    {
                        P1 = 2 * value[2] - 0.5f * (P0 + P2);
                    }
                }
            }

            if (IsValid)
            {
                // Auto adjust invalid P1
                if (float.IsInfinity(P1.x) || Algebra.Parallel(P2 - P1, P1 - P0) ||
                (P2 - P1).magnitude / (P1 - P0).magnitude > 3 || (P2 - P1).magnitude / (P1 - P0).magnitude < 1f / 3)
                {
                    P1 = (P2 + P0) / 2 + Algebra.RotatedY((P2 - P0) / 2, -Mathf.PI / 2);
                }

                NotifyShapeChanged();
            }
        }
    }

    protected override float _GetLength()
    {
        return Mathf.Abs(lengthIntegral(t_end) - lengthIntegral(t_start));
    }

    protected override Vector2 _GetFrontDir(float t)
    {
        t = toGlobalParam(t);
        Vector2 global_front = (2 * (t - 1) * P0 + (2 - 4 * t) * P1 + 2 * t * P2).normalized;
        return t_end > t_start ? global_front : -global_front;
    }

    protected override Vector2 _GetTwodPos(float t)
    {
        t = toGlobalParam(t);
        return (1 - t) * (1 - t) * P0 + 2 * t * (1 - t) * P1 + t * t * P2;
    }

    protected override float? _ParamOf(Vector2 p)
    {
        float A11 = (P0.y - 2 * P1.y + P2.y) * 2 * (P1.x - P0.x);
        float A12 = (P0.x - 2 * P1.x + P2.x) * 2 * (P1.y - P0.y);
        float A1 = A11 - A12;

        float A01 = (P0.y - 2 * P1.y + P2.y) * (P0.x - p.x);
        float A02 = (P0.x - 2 * P1.x + P2.x) * (P0.y - p.y);
        float A0 = A01 - A02;

        float realParam = toLocalParam(-A0 / A1);

        if (!Algebra.isclose(GetTwodPos(_ToUnscaledt(realParam)), p))
        {
            return null;
        }

        return Algebra.approximateTo01(realParam, Length);
    }

    protected override float _ToParamt(float unscaled_t)
    {
        return Algebra.NewTown(t => _ToUnscaledt(t) * Length, lengthGradient, unscaled_t * Length, 1);
    }

    protected override float _ToUnscaledt(float t)
    {
        float world_start_t = toGlobalParam(0);
        float world_end_t = toGlobalParam(t);
        return Mathf.Sign(t) * Mathf.Abs(lengthIntegral(world_end_t) - lengthIntegral(world_start_t)) / Length;
    }

    float lengthIntegral(float world_t)
    {
        float A1 = 2 * P2.x - 4 * P1.x + 2 * P0.x;
        float A0 = -2 * P0.x + 2 * P1.x;
        float B1 = 2 * P2.y - 4 * P1.y + 2 * P0.y;
        float B0 = -2 * P0.y + 2 * P1.y;
        /*int(sqrt(Ax2+Bx+C))*/
        float A = A1 * A1 + B1 * B1;
        float B = 2 * A1 * A0 + 2 * B1 * B0;
        float C = A0 * A0 + B0 * B0;
        if (Algebra.isclose(A, 0f))
        {
            return (P2 - P0).magnitude;
        }
        else
        {
            /*http://www.wolframalpha.com/input/?i=integral+sqrt(Ax%5E2%2BBx%2BC) */
            float firstItem = (2 * A * world_t + B) * Mathf.Sqrt(world_t * (A * world_t + B) + C) / (4 * A);
            float secondItem = (B * B - 4 * A * C) * Mathf.Log(2 * Mathf.Sqrt(A) * Mathf.Sqrt(world_t * (A * world_t + B) + C) + 2 * A * world_t + B) / (8 * Mathf.Pow(A, 1.5f));
            return firstItem - secondItem;
        }
    }


    float lengthGradient(float local_t)
    {
        local_t = toGlobalParam(local_t);
        Vector2 dxy_dt = 2 * (local_t - 1) * P0 + (2 - 4 * local_t) * P1 + 2 * local_t * P2;
        return dxy_dt.magnitude * Mathf.Abs(t_end - t_start);
    }

    public override float GetMaximumCurvature => (P2 + P0 - 2 * P1).magnitude / Mathf.Abs((P1.x - P0.x) * (P2.y - P1.y) - (P1.y - P0.y) * (P2.x - P1.x));


    public override Curve Clone()
    {
        Bezier copy = new Bezier(P0, P1, P2);
        copy.t_start = t_start;
        copy.t_end = t_end;
        return copy;
    }

    public override void ShiftRight(float distance)
    {
        Line P0_P1_ray = new Line(P0, P0 + _GetFrontDir(toLocalParam(0f)) * Algebra.InfLength);
        Line P2_P1_ray = new Line(P2, P2 - _GetFrontDir(toLocalParam(1f)) * Algebra.InfLength);
        P0_P1_ray.ShiftRight(distance);
        P2_P1_ray.ShiftRight(-distance);

        var Intersection = P0_P1_ray.IntersectWith(P2_P1_ray);
        if (Intersection.Count == 0)
        {
            Invalidate();
        }
        else
        {
            P0 = P0_P1_ray.GetTwodPos(0f);
            P2 = P2_P1_ray.GetTwodPos(0f);
            P1 = Intersection[0];
            NotifyShapeChanged();
        }


    }

    public override string ToString()
    {
        return "Bezeir P0: " + P0 + " ,P1: " + P1 + " ,P2: " + P2 + 
        " with t_start: " + t_start + " ,t_end = " + t_end;
    }
}
