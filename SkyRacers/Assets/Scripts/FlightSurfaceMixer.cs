using System;

namespace SkyRacers
{
    public enum FlightSurfaceRole { Stabilator, Rudder, Aileron, TrailingFlap }

    public static class FlightSurfaceMixer
    {
        public static float AutoFlap(float speed, float angleOfAttack)
        {
            float slow = FlightDynamics.Clamp((180 - speed) / 90, 0, 1) * .65f;
            float alpha = FlightDynamics.Clamp((angleOfAttack - 4) / 20, 0, 1);
            return Math.Max(slow, alpha);
        }

        // side = -1 on the left and +1 on the right. Positive hinge-X raises
        // the trailing edge; positive rudder angle moves it toward local -X.
        public static float Deflection(FlightSurfaceRole role, int side, float pitch, float roll, float yaw, float brake, float flap)
        {
            float angle;
            switch (role)
            {
                case FlightSurfaceRole.Stabilator: angle = -pitch * 24 - side * roll * 14 + brake * 10; break;
                case FlightSurfaceRole.Rudder: angle = -yaw * 26 + side * brake * 22; break;
                case FlightSurfaceRole.Aileron: angle = -side * roll * 26 - flap * 4 - brake * 8; break;
                default: angle = -flap * 18 - side * roll * 8 - brake * 22; break;
            }
            return FlightDynamics.Clamp(angle, -35, 35);
        }
    }
}
