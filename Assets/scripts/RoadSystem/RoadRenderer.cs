﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


//objects rendered in discontinues manner
class NonLinear3DObject{
    public GameObject obj;
    public float interval;
}

/*should support:
lane 
interval
surface_{width}
removal_{width}

yellow/white/blueindi_dash/solid

should also support:
barrier
*/
public class RoadRenderer : MonoBehaviour
{

    public GameObject rend;
    public static float laneWidth = 2.8f;
    public static float separatorWidth = 0.2f;
    public static float separatorInterval = 0.2f;
    public static float fenceWidth = 0.2f;

    public float dashLength = 2.5f;
    public float dashInterval = 2.5f;

    public float dashIndicatorLength = 1f;
    public float dashIndicatorWidth = 2f;

    static List<string> commonTypes = new List<string> { "lane", "interval", "surface", "removal", "fence", "column", "singlefence" };

    public static List<string> separatorTypes = new List<string> { "solid_yellow", "dash_yellow", "solid_white", "solid_yellow" };

    public void generate(Road r){
        generate(r.curve, r.laneconfigure, r.margin0LLength, r.margin0RLength, r.margin1LLength, r.margin1RLength);
    }

    public void generate(Curve curve, List<string> laneConfig,
                         float indicatorMargin_0L = 0f, float indicatorMargin_0R = 0f, float indicatorMargin_1L = 0f, float indicatorMargin_1R = 0f){
                         
        float indicatorMargin_0Bound = Mathf.Max(indicatorMargin_0L, indicatorMargin_0R);
        float indicatorMargin_1Bound = Mathf.Max(indicatorMargin_1L, indicatorMargin_1R);
        if (indicatorMargin_0Bound + indicatorMargin_1Bound >= curve.length)
        {
            throw new System.Exception();
        }

        Curve[] fragments = splitByMargin(curve, indicatorMargin_0Bound, indicatorMargin_1Bound);
        if (fragments[0] != null){
            generateSingle(fragments[0], laneConfig, indicatorMargin_0L, indicatorMargin_0R, 0f, 0f);
        }
        if (fragments[1] != null){
            generateSingle(fragments[1], laneConfig, 0f, 0f, 0f, 0f);
        }
        if (fragments[2] != null){
            generateSingle(fragments[2], laneConfig, 0f, 0f, indicatorMargin_1L, indicatorMargin_1R);
        }
    }

    public static Curve[] splitByMargin(Curve curve, float indicatorMargin_0Bound, float indicatorMargin_1Bound)
    {
        Curve[] rtn = new Curve[3];

        if (indicatorMargin_0Bound > 0)
        {
            Curve margin0Curve = curve.cut(0f, indicatorMargin_0Bound / curve.length);
            margin0Curve.z_start = curve.At(0f).y;
            margin0Curve.z_offset = 0f;
            rtn[0] = margin0Curve;
        }

        Curve middleCurve = curve.cut(indicatorMargin_0Bound / curve.length, 1f - indicatorMargin_1Bound / curve.length);
        middleCurve.z_start = curve.At(0f).y;
        middleCurve.z_offset = curve.At(1f).y - curve.At(0f).y;
        rtn[1] = middleCurve;

        if (indicatorMargin_1Bound > 0)
        {
            Curve margin1Curve = curve.cut(1f - indicatorMargin_1Bound / curve.length, 1f);
            margin1Curve.z_start = curve.At(1f).y;
            margin1Curve.z_offset = 0f;
            rtn[2] = margin1Curve;
        }
        return rtn;
    }

    void generateSingle(Curve curve, List<string> laneConfig, 
                        float indicatorMargin_0L , float indicatorMargin_0R, float indicatorMargin_1L, float indicatorMargin_1R)
    {
        List<Linear2DObject> separators = new List<Linear2DObject>();
        List<Linear3DObject> linear3DObjects = new List<Linear3DObject>();
        float width = getConfigureWidth(laneConfig);

        for (int i = 0; i != laneConfig.Count; ++i)
        {
            string l = laneConfig[i];
            float partialWidth = getConfigureWidth(laneConfig.GetRange(0, i + 1));
            string[] configs = l.Split('_');
            if (commonTypes.Contains(configs[0]))
            {
                switch (configs[0])
                {
                    case "lane":
                    case "interval":
                        if (!linear3DObjects.Any(obj=>obj.tag == "bridgepanel")){
                            linear3DObjects.Add(new Linear3DObject("bridgepanel"));
                        }
                        if (!linear3DObjects.Any(obj=>obj.tag == "crossbeam")){
                            Linear3DObject squarecolumn = new Linear3DObject("squarecolumn");
                            Linear3DObject crossbeam = new Linear3DObject("crossbeam");
                            squarecolumn.margin_0 = crossbeam.margin_0 =
                                Algebra.Lerp(indicatorMargin_0L, indicatorMargin_0R, partialWidth / width);
                            squarecolumn.margin_1 = crossbeam.margin_1 =
                                Algebra.Lerp(indicatorMargin_1L, indicatorMargin_1R, partialWidth / width);

                            linear3DObjects.Add(squarecolumn);
                            linear3DObjects.Add(crossbeam);
                        }
                        break;

                    case "fence":
                        {
                            Linear3DObject fence = new Linear3DObject("fence");
                            Linear3DObject lowbar = new Linear3DObject("lowbar");
                            Linear3DObject highbar = new Linear3DObject("highbar");

                            fence.offset = lowbar.offset = highbar.offset = partialWidth - fenceWidth;
                            fence.margin_0 = lowbar.margin_0 = highbar.margin_0 =
                                Algebra.Lerp(indicatorMargin_0L, indicatorMargin_0R, partialWidth / width);
                            fence.margin_1 = lowbar.margin_1 = highbar.margin_1 =
                                Algebra.Lerp(indicatorMargin_1L, indicatorMargin_1R, partialWidth / width);

                            linear3DObjects.Add(fence);
                            linear3DObjects.Add(lowbar);
                            linear3DObjects.Add(highbar);
                            break;
                        }
                    case "singlefence":
                        {
                            Linear3DObject fence = new Linear3DObject("fence");
                            Linear3DObject lowbar = new Linear3DObject("lowbar");
                            Linear3DObject highbar = new Linear3DObject("highbar");

                            fence.offset = lowbar.offset = highbar.offset = partialWidth - fenceWidth * 1.5f;
                            fence.margin_0 = lowbar.margin_0 = highbar.margin_0 =
                                Algebra.Lerp(indicatorMargin_0L, indicatorMargin_0R, partialWidth / width);
                            fence.margin_1 = lowbar.margin_1 = highbar.margin_1 =
                                Algebra.Lerp(indicatorMargin_1L, indicatorMargin_1R, partialWidth / width);

                            linear3DObjects.Add(fence);
                            linear3DObjects.Add(lowbar);
                            linear3DObjects.Add(highbar);
                            break;
                        }

                    case "removal":
                        Debug.Assert(laneConfig.Count == 1);
                        //drawRemovalMark(curve, float.Parse(configs[1]));
                        Linear2DObject sep = new Linear2DObject("removal");
                        sep.dashed = false;
                        sep.width = float.Parse(configs[1]);
                        sep.margin_0 = Mathf.Max(indicatorMargin_0L, indicatorMargin_0R);
                        sep.margin_1 = Mathf.Max(indicatorMargin_1L, indicatorMargin_1R);
                        drawLinear2DObject(curve, sep);
                        break;
                }
            }
            else
            {
                string septype, sepcolor;
                septype = configs[0];
                sepcolor = configs[1];

                Linear2DObject sep;

                switch (sepcolor)
                {
                    case "yellow":
                        sep = new Linear2DObject("yellow");
                        break;
                    case "white":
                        sep = new Linear2DObject("white");
                        break;
                    case "blueindi":
                        sep = new Linear2DObject("blueindi");
                        break;
                    default:
                        throw new System.Exception();
                }

                sep.margin_0 = Algebra.Lerp(indicatorMargin_0L, indicatorMargin_0R, partialWidth / width);
                sep.margin_1 = Algebra.Lerp(indicatorMargin_1L, indicatorMargin_1R, partialWidth / width);

                switch (septype)
                {
                    case "dash":
                        sep.dashed = true;
                        break;
                    case "solid":
                        sep.dashed = false;
                        break;
                }

                sep.offset = partialWidth - separatorWidth;

                separators.Add(sep);
            }

        }

        //adjust center

        for (int i = 0; i != separators.Count; i++)
        {
            separators[i].offset -= (width / 2 - separatorWidth / 2);
            drawLinear2DObject(curve, separators[i]);
        }


        for (int i = 0; i != linear3DObjects.Count; i++)
        {
            {
                Linear3DObject obj = linear3DObjects[i];
                switch(obj.tag){
                    case "crossbeam":
                        if ((curve.z_start > 0 || curve.z_offset > 0)) {
                            linear3DObjects[i].setParam(width);
                            drawLinear3DObject(curve, linear3DObjects[i]);
                        }
                        break;
                    case "squarecolumn":
                        if ((curve.z_start > 0 || curve.z_offset > 0)){
                            drawLinear3DObject(curve, linear3DObjects[i]);
                        }
                        break;
                    case "bridgepanel":
                        linear3DObjects[i].setParam(width);
                        drawLinear3DObject(curve, linear3DObjects[i]);

                        break;
                    case "fence":
                    case "singlefence":
                    case "highbar":
                    case  "lowbar":
                        linear3DObjects[i].offset -= (width / 2 - fenceWidth / 2);
                        drawLinear3DObject(curve, linear3DObjects[i]);

                        break;
                }
            }
        }

    }

    void drawLinear2DObject(Curve curve, Linear2DObject sep){
        float margin_0 = sep.margin_0;
        float margin_1 = sep.margin_1;
        if (Algebra.isclose(margin_0 + margin_1, curve.length)){
            return;
        }
        Debug.Assert(margin_0 + margin_1 < curve.length);
        if (margin_0 + margin_1 > curve.length){
            return;
        }

        if (curve.length > 0 && (margin_0 > 0 || margin_1 > 0))
        {
            curve = curve.cut(margin_0 / curve.length, 1f - margin_1 / curve.length);
        }
		if (!sep.dashed) {
			GameObject rendins = Instantiate (rend, transform);
			CurveRenderer decomp = rendins.GetComponent<CurveRenderer> ();
            decomp.CreateMesh (curve, sep.width, sep.material, offset: sep.offset, z_offset:0.02f);
		}
		else {
            List<Curve> dashed = curve.segmentation (dashLength + dashInterval);
            if (!Algebra.isclose(dashed.Last().length, dashLength + dashInterval)){
                dashed.RemoveAt(dashed.Count - 1);
            }
			foreach (Curve singledash in dashed) {
                List<Curve> vacant_and_dashed = singledash.split(dashInterval / (dashLength + dashInterval));

                if (vacant_and_dashed.Count == 2) {
					GameObject rendins = Instantiate (rend, transform);
					CurveRenderer decomp = rendins.GetComponent<CurveRenderer> ();
                    decomp.CreateMesh (vacant_and_dashed [1], sep.width, sep.material, offset:sep.offset, z_offset:0.02f);
				}
			}

		}
	}

    void drawLinear3DObject(Curve curve, Linear3DObject obj){
        float margin_0 = obj.margin_0;
        float margin_1 = obj.margin_1;
        if (Algebra.isclose(margin_0 + margin_1, curve.length))
        {
            return;
        }
        Debug.Assert(margin_0 >= 0 && margin_1 >= 0);
        /*
        if (margin_0 + margin_1 >= curve.length){
            Debug.LogError(margin_0 + " ; " + margin_1 + " >= " + curve.length + " at: " + curve);
        }
        */
        if (margin_0 + margin_1 > curve.length)
        {
            return;
        }

        if (curve.length > 0 && (margin_0 > 0 || margin_1 > 0)){
            curve = curve.cut(margin_0 / curve.length, 1f - margin_1 / curve.length);
        }
        if (obj.dashInterval == 0f)
        {
            GameObject rendins = Instantiate(rend, transform);
            rendins.transform.parent = this.transform;
            CurveRenderer decomp = rendins.GetComponent<CurveRenderer>();
            decomp.CreateMesh(curve, obj.offset, obj.linearMaterial, obj.crossMaterial, obj.cross_section);
        }
        else
        {
            Debug.Assert(obj.dashLength > 0f);
            List<Curve> dashed = curve.segmentation(obj.dashLength + obj.dashInterval);
            if (!Algebra.isclose(dashed.Last().length, dashLength + dashInterval))
            {
                dashed.RemoveAt(dashed.Count - 1);
            }
            foreach (Curve singledash in dashed)
            {
                List<Curve> vacant_and_dashed = singledash.split(obj.dashInterval / (obj.dashLength + obj.dashInterval));
                if (vacant_and_dashed.Count == 2)
                {
                    GameObject rendins = Instantiate(rend, transform);
                    rendins.transform.parent = this.transform;
                    CurveRenderer decomp = rendins.GetComponent<CurveRenderer>();
                    decomp.CreateMesh(vacant_and_dashed[1], obj.offset, obj.linearMaterial, obj.crossMaterial, obj.cross_section);
                }
            }
        }
    }

    public void generate(Polygon polygon, float H, string materialconfig)
    {
        GameObject rendins = Instantiate(rend, transform);
        rendins.transform.parent = this.transform;
        CurveRenderer decomp = rendins.GetComponent<CurveRenderer>();
        switch (materialconfig)
        {
            default:
                decomp.CreateMesh(polygon, H, Resources.Load<Material>("Materials/roadsurface"));
                break;
        }
    }

    public static float getConfigureWidth(List<string> laneconfigure)
    {
        var ans = 0f;
        for (int i = 0; i != laneconfigure.Count; ++i)
        {
            if (commonTypes.Contains(laneconfigure[i].Split('_')[0]))
                switch (laneconfigure[i].Split('_')[0])
                {
                    case "lane":
                        ans += laneWidth;
                        break;
                    case "interval":
                        ans += separatorInterval;
                        break;
                    case "fence":
                    case "singlefence":
                        ans += fenceWidth;
                        break;
                }
            else
            {
                ans += separatorWidth;
            }
        }
        return ans;
    }
}
