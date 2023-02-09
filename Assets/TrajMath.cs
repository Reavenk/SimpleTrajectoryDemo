using UnityEngine;

public static class TrajMath
{
    /// <summary>
    /// Given a target offset and a launch strength, find the angles to fire
    /// </summary>
    /// <param name="power">The launch power of the particle.</param>
    /// <param name="gravity">Vertical gravity.</param>
    /// <param name="dist">Horizontal offset of the target from the shooter.</param>
    /// <param name="above">Vertical offset of the target from the shooter.</param>
    /// <param name="usePos">Whether to return the positive or negative value.</param>
    /// <returns>
    /// The angle, in degrees, to shoot a particle from the shooter to hit the target.
    /// 
    /// An error value of NaN is returned if the power is insufficient.
    /// </returns>
    public static float GetShootingAngle(float power, float gravity, float dist, float above, bool usePos)
    {
        float v = power;
        float v2 = v * v;
        float v4 = v2 * v2;
        float g = gravity;
        float x = dist;
        float y = above;

        float insideSqrt = v4 + g * (-g * x * x + 2 * y * v2);

        // If the insideSqrt is less than 0, we're going to end up attempting
        // to get the sqrt of a negative number. This happens if the launch
        // power isn't enough to get us to our target. In which case there is
        // no solution.
        if (insideSqrt < 0.0f)
            return float.NaN;

        if (usePos == true)
        {
            float tanthP = (v2 + Mathf.Sqrt(insideSqrt)) / (-g * x);
            return Mathf.Rad2Deg * Mathf.Atan(tanthP);
        }
        else
        {
            float tanthN = (v2 - Mathf.Sqrt(insideSqrt)) / (-g * x);
            return Mathf.Rad2Deg * Mathf.Atan(tanthN);
        }
    }

    /// <summary>
    /// Given a certain time (t), as well as some other variables, calculate
    /// where a ballistically launched particle will be.
    /// </summary>
    /// <param name="t">The time (in seconds) to calculate for.</param>
    /// <param name="shootVec">
    /// The launch vector of the particle. Where X is left/right, and Y is vertical.
    /// This is a normalized vector and should have a magnitude of 1.
    /// </param>
    /// <param name="shooterY">The starting height of the particle.</param>
    /// <param name="power">The launch power (initial velocity) of the particle.</param>
    /// <param name="gravity">Vertical gravity.</param>
    public static Vector2 PredictLaunchAtTime(float t, Vector2 shootVec, float shooterY, float power, float gravity)
    {
        // https://en.wikipedia.org/wiki/Equations_of_motion
        float x = shootVec.x * power * t;
        float r0 = shooterY;
        float a = gravity;
        float vt = shootVec.y * power;

        float r = r0 + vt * t + 0.5f * a * t * t;
        return new Vector2(x, r);
    }
}
