using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    /// <summary>
    /// The type of automatch to update the trajectory with.
    /// </summary>
    public enum MatchType
    { 
        /// <summary>
        /// Don't do any auto-updating of the trajectory.
        /// </summary>
        None,

        /// <summary>
        /// Use the positive value.
        /// </summary>
        Positive,

        /// <summary>
        /// Use the negative value.
        /// </summary>
        Negative
    }

    public GameObject shooter;                  // The tank body shooting bullets
    public GameObject bulletPrefab;             // The bullet to shoot
    public GameObject targetPreview;            // The target to aim for

    GameObject trajPreview;                     // The GameObject showing the trajectory preview curve.
    public Material trajPreviewMat;             // The curve that the trajectory preview is rendered with when possible.
    public Material trajPreviewMatErr;          // The curve that the trajectory preview is rendered with when impossible.
    public Material trajPreviewMatMatch;        // The curve that the trajectory preview is rendered with when matched.
    Mesh trajMesh = null;                       // The mesh rendering the trajectory preview curve line segments.
    MeshRenderer meshRenderer = null;   

    const float minPower = 0.0f;                // The minimum allowed power for the tank
    const float maxPower = 100.0f;              // The maximum allowed power for the tank

    const float minAngle = 0.0f;                // The minimum allowed angle for the tank
    const float maxAngle = 90.0f;               // The maximum allowed angle for the tank

    const float minDist = 10.0f;                // The minimum allowed distance to place the target
    const float maxDist = 80.0f;                // The maximum allowed distance to place the target.

    const float minEle = 0.0f;                  // The minimum allowed target elevation.
    const float maxEle = 30.0f;                 // The maximum allowed target elevation.

    
    float angle = (minAngle + maxAngle)/2.0f;   // The current firing angle for the tank
    float power = (minPower + maxPower)/2.0f;   // The current firing power for the tank
    float dist = (minDist + maxDist)/2.0f;      // The current distance of the target
    float ele = (minEle + maxEle)/2.0f;         // The current elevation of the target

    const int PrevSamplesPerSecond = 20;        // Number of samples per second to show the trajectory preview for
    const int SecondsOfPreview = 10;            // The number of seconds to show the trajectory preview for
    Vector3 [] samplesPoints;                   // Cached trajectory points

    MatchType automatch = MatchType.None;       // What type of per-frame trajectory updating to do.

    public UnityEngine.UI.Text lowPowerErrText; // Error UI shown when the power level is too low to hit the target.

    // Alpha value for lowPowerErrText. Used to show a constant error when
    // tracking, or to show a temporary error when Match is pressed.
    float errShowAlpha = 0.0f;  
    
    bool angleEnabled = true;                   // Tracks if the Angle slider and Match button are enabled

    private void Start()
    {
        // Create the trajectory curve preview.
        this.trajPreview = new GameObject("Trajectory Preview");
        this.meshRenderer = this.trajPreview.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = this.trajPreviewMat;
        MeshFilter mf = this.trajPreview.AddComponent<MeshFilter>();

        int totalSamples = PrevSamplesPerSecond * SecondsOfPreview;
        this.samplesPoints = new Vector3[totalSamples];
        List<int> indices = new List<int>();
        for(int i = 0; i < totalSamples; ++i)
        { 
            this.samplesPoints[i] = Vector3.zero;
            indices.Add(i);
        }
        this.trajMesh = new Mesh();
        this.trajMesh.SetVertices(samplesPoints);
        this.trajMesh.SetIndices( indices, MeshTopology.LineStrip, 0);
        mf.mesh = this.trajMesh;
    }

    // Update is called once per frame
    private void Update()
    {
        // If "Automatch[-]" or "Automatch[+]" are enabled, recalculate the
        // shooting angle.
        this.angleEnabled = true;
        float ? newAngle = null;
        if(this.automatch == MatchType.Negative)
        {
            newAngle = this.GetShootingAngle(false);
            
        }
        else if(this.automatch == MatchType.Positive)
        {
            newAngle = this.GetShootingAngle(true);
        }

        if(newAngle.HasValue == true)
        {

            if (float.IsNaN(newAngle.Value) == false)
            {
                this.meshRenderer.sharedMaterial = this.trajPreviewMatMatch;
                this.angle = newAngle.Value;
                this.errShowAlpha = 0.0f;
                this.angleEnabled = false;
            }
            else
            {
                this.meshRenderer.sharedMaterial = this.trajPreviewMatErr;
                this.errShowAlpha = 1.0f;
            }
        }
        else
        { 
            this.errShowAlpha = Mathf.Clamp01(this.errShowAlpha - Time.deltaTime * 0.5f);
            this.meshRenderer.sharedMaterial = this.trajPreviewMat;
        }

        this.lowPowerErrText.color = SetAlpha(this.lowPowerErrText.color, this.errShowAlpha);

        // Match the tank's rotation upwards to match our shooting angle.
        this.shooter.transform.rotation =
            Quaternion.Euler(new Vector3(-this.angle, 0.0f, 0.0f));

        // Sync the target's GameObject to match our controls for it.
        targetPreview.transform.position = new Vector3(0.0f, this.ele, this.dist);

        // Recalculate the trajectory preview.
        this.RebuildTrajectory();
    }

    static Color SetAlpha(Color c, float a)
    { 
        return new Color(c.r, c.g, c.b, a);
    }

    // Recalculate and set the vertices for the trajectory curve preview.
    public void RebuildTrajectory()
    {
        for (int i = 0; i < this.samplesPoints.Length; ++i)
        {
            float t = (float)i / (float)PrevSamplesPerSecond;
            Vector2 cur = 
                TrajMath.PredictLaunchAtTime(
                    t, 
                    new Vector2(
                        this.shooter.transform.forward.z, 
                        this.shooter.transform.forward.y), 
                    this.shooter.transform.position.y, 
                    this.power, 
                    Physics.gravity.y);

            this.samplesPoints[i] = new Vector3(0.0f, cur.y, cur.x);
        }

        this.trajMesh.SetVertices(this.samplesPoints);
    }

    /// <summary>
    /// Delegate to TrajMath.GetShootingAngle() with the correct member variables
    /// used as parameters.
    /// </summary>
    /// <param name="usePos">
    /// See parameter with same name for TrajMath.GetShootingAngle for more details.
    /// </param>
    /// <returns>
    /// The angle to set the tank to hit the target.
    /// 
    /// Note that TrajMath.GetShootingAngle may return float.NaN if the power isn't
    /// strong enough to hit the target.
    /// </returns>
    float GetShootingAngle(bool usePos)
    {
        return
            TrajMath.GetShootingAngle(
                this.power,
                Physics.gravity.y, 
                this.dist,
                this.targetPreview.transform.position.y - shooter.transform.position.y,
                usePos);
    }

    void ShootBox()
    {
        GameObject go = GameObject.Instantiate(bulletPrefab);
        Rigidbody rb = go.GetComponent<Rigidbody>();

        // Match the rotation of the tank.
        go.transform.rotation = shooter.transform.rotation;
        go.transform.position = shooter.transform.position;

        // Assign the physics used by unity to use the same angle and power
        // we've been previewing our trajectory curve with.
        rb.velocity = this.shooter.transform.forward * this.power;
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal("box");
                GUILayout.FlexibleSpace();
                GUILayout.Label("Target");
                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Distance");
            this.dist = GUILayout.HorizontalSlider(this.dist, minDist, maxDist, GUILayout.Width(200.0f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Elevation");
            this.ele = GUILayout.HorizontalSlider(this.ele, minEle, maxEle, GUILayout.Width(200.0f));
            GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(50.0f);

        GUI.enabled = this.angleEnabled;
        GUILayout.BeginVertical("box");

            if (this.automatch != MatchType.None)
                GUI.enabled = false;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Angle"); 
            this.angle = GUILayout.HorizontalSlider(this.angle, minAngle, maxAngle, GUILayout.Width(200.0f));
            GUILayout.EndHorizontal();

            // A button that can be pressed to set the angle to hit the target. Hitting the
            // button while it's already on a valid solution will toggle to the 
            // other (positive/negative) solution.
            if (GUILayout.Button("Match") == true)
            {

                float a = GetShootingAngle(true);
                if (float.IsNaN(a) == false)
                {
                    if (a != this.angle)
                        this.angle = a;
                    else
                        this.angle = GetShootingAngle(false);
                }
                else
                {
                    // If not enough power to hit target, raise the alpha value for the
                    // error message and it will briefly flash.
                    this.errShowAlpha = 1.0f;
                }
            }
            GUI.enabled = true;

            GUILayout.BeginHorizontal();

            bool neg = (this.automatch == MatchType.Negative);
            bool negSel = GUILayout.Toggle(neg, "Automatch[-]");
            if (neg != negSel)
            {
                if (negSel == true)
                    this.automatch = MatchType.Negative;
                else
                    this.automatch = MatchType.None;
            }

            bool pos = (this.automatch == MatchType.Positive);
            bool posSel = GUILayout.Toggle(pos, "Automatch[+]");
            if (pos != posSel)
            {
                if (posSel == true)
                    this.automatch = MatchType.Positive;
                else
                    this.automatch = MatchType.None;
            }
            GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUI.enabled = true;

        GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();
            GUILayout.Label("Power");
            this.power = GUILayout.HorizontalSlider(this.power, minPower, maxPower, GUILayout.Width(200.0f));
        GUILayout.EndHorizontal();
        

        if (GUILayout.Button("Shoot", GUILayout.Height(30.0f)) == true)
            this.ShootBox();
    }
}
