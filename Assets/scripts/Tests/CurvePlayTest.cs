﻿using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class CurvePlayTest
    {
        Dictionary<Vector2, Color> POI;

        Vector2[] roadPoints;
        Curve c, b, l, l2;
        GameObject lightGameObject;

        [SetUp]
        public void CreateTests()
        {
            roadPoints = new Vector2[]{
                new Vector2(0f, 0f),
                new Vector2(0f, 20f),
                new Vector2(-30f, 0f),
                new Vector2(-20f, 20f)
            };
            POI = new Dictionary<Vector2, Color>();

            c = new Arc(roadPoints[1], roadPoints[0], -Mathf.PI / 2);
            b = new Bezier(roadPoints[2], roadPoints[0], roadPoints[1]);
            l = new Line(roadPoints[0], roadPoints[3]);


            if (lightGameObject == null)
            {
                lightGameObject = new GameObject("The Light");

                Light lightComp = lightGameObject.AddComponent<Light>();
                lightComp.color = Color.white;
                lightComp.type = LightType.Directional;
                lightGameObject.transform.position = new Vector3(0, 50, 0);
                lightGameObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

        }

        [Test]
        public void SegmentationTest()
        {
            Debug.Log("Bezeir Test...");
            for (float i = 0; i < 0.9f; i += 0.1f)
            {
                Curve bi = b.Clone();
                bi.Crop(1.0f, 0.0f); //Inverse
                bi.Crop(i, i + 0.1f);
                Debug.Log(bi + "\n" + bi.GetTwodPos(i));
                POI.Add(bi.GetTwodPos(0.5f), Color.white);
            }
            Debug.Log("Arc Test...");
            for (float i = 0; i < 0.9f; i += 0.1f)
            {
                Curve ci = c.Clone();
                ci.Crop(i, i + 0.1f);
                Debug.Log(ci + "\n" + ci.GetTwodPos(i));
                POI.Add(ci.GetTwodPos(0.5f), Color.white);
            }

            Debug.Log("Line Test...");
            for (float i = 0; i < 0.9f; i += 0.1f)
            {
                Curve li = l.Clone();
                li.Crop(i, i + 0.1f);
                Debug.Log(li + "\n" + li.GetTwodPos(i));
                POI.Add(li.GetTwodPos(0.5f), Color.white);
            }
        }

        [Test]
        public void IntersectionTest()
        {
            Debug.Log("c & b...");
            var inter = c.IntersectWith(b);
            foreach (var i in inter)
            {
                POI.Add(i, Color.yellow);
            }

            Debug.Log("l & c...");
            inter = l.IntersectWith(c);
            foreach (var i in inter)
            {
                POI.Add(i, Color.yellow);
            }

            Debug.Log("l & b...");
            inter = l.IntersectWith(b);
            foreach (var i in inter)
            {
                POI.Add(i, Color.yellow);
            }

        }

        [Test]
        public void ParamofTest()
        {
            Debug.Log(b.ParamOf(b.GetTwodPos(-0.5f)));
            Debug.Log(b.ParamOf(b.GetTwodPos(1.5f)));
        }

        [Test]
        public void ShiftTest()
        {
            b.Crop(0.1f, 0.5f);
            var bb = b.Clone();
            bb.ShiftRight(1f);
            bb.ShiftRight(-1f);
            Debug.Log(Curve.sameMotherCurve(bb, b));
        }


        [TearDown]
        public void EndTest()
        {
            foreach(var loc in POI.Keys)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                c.transform.position = Algebra.toVector3(loc);
                c.transform.localScale = Vector3.one * 0.5f;
                c.GetComponent<MeshRenderer>().material.color = POI[loc];
            }
        }

        [UnityTest]
        public IEnumerator Z_CurvePlayTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return new WaitForSeconds(10);
        }
    }
}
