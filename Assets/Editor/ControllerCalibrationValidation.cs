using System;
using UnityEngine;

namespace ControllerUdp.Editor
{
    public static class ControllerCalibrationValidation
    {
        public static void Validate()
        {
            var frame = new ControllerCalibration();
            Quaternion heading = Quaternion.Euler(0, 90, 0);
            Vector3 origin = new Vector3(2, 1, 3);
            Require(frame.Update(true, true, origin - Vector3.forward, origin, heading), "first press");
            Near(frame.Position(origin), Vector3.zero, "right-controller origin");
            Near(frame.Position(origin + Vector3.right), Vector3.right, "right-controller forward becomes +X");
            Near(frame.Position(origin + Vector3.forward), Vector3.up, "left becomes +Y");
            Near(frame.Position(origin + Vector3.up), Vector3.forward, "up becomes +Z");
            Near(frame.Velocity(Vector3.right), Vector3.right, "velocity has no origin offset");
            Near(frame.AngularVelocity(Vector3.up), Vector3.back, "axial handedness conversion");
            Require(Quaternion.Angle(frame.Rotation(heading), Quaternion.identity) < 0.001f, "heading rotation becomes identity");
            Quaternion pose = Quaternion.Euler(23, -41, 67);
            Vector3 vector = new Vector3(0.3f, -0.7f, 1.1f);
            Near(frame.Rotation(pose) * ControllerCalibration.Polar(vector),
                frame.Velocity(pose * vector), "rotation basis conjugation");
            Require(!frame.Update(true, true, Vector3.zero, Vector3.zero, Quaternion.identity) && frame.Revision == 1,
                "holding both grips does not recalibrate");
            frame.Update(false, true, Vector3.zero, Vector3.zero, Quaternion.identity);
            Require(frame.Update(true, true, Vector3.zero, Vector3.zero, Quaternion.identity) && frame.Revision == 2,
                "release and press recalibrates");
            frame.Update(false, true, Vector3.zero, Vector3.zero, Quaternion.identity);
            Require(!frame.Update(true, false, Vector3.zero, Vector3.zero, Quaternion.identity), "untracked press rejected");
            frame.Update(false, true, Vector3.zero, Vector3.zero, Quaternion.identity);
            Quaternion vertical = Quaternion.Euler(90, 0, 0);
            Require(frame.Update(true, true, origin - Vector3.one, origin, vertical), "vertical pose accepted");
            Require(Quaternion.Angle(frame.Rotation(vertical), Quaternion.identity) < 0.001f, "vertical calibration rotation zero");
            frame.Update(false, true, Vector3.zero, Vector3.zero, Quaternion.identity);
            Quaternion tilted = Quaternion.Euler(32, 71, -43);
            Require(frame.Update(true, true, origin - Vector3.one, origin, tilted), "pitch yaw roll calibration");
            Near(frame.Position(origin + tilted * Vector3.forward), Vector3.right, "tilted forward becomes +X");
            Near(frame.Position(origin + tilted * Vector3.left), Vector3.up, "tilted left becomes +Y");
            Near(frame.Position(origin + tilted * Vector3.up), Vector3.forward, "tilted up becomes +Z");
            Require(Quaternion.Angle(frame.Rotation(tilted), Quaternion.identity) < 0.001f, "full calibration rotation zero");
            Near(frame.Rotation(pose) * ControllerCalibration.Polar(vector),
                frame.Velocity(pose * vector), "tilted rotation basis conjugation");
            Require(frame.Revision == 4, "calibration revision count");
            var correctionFrame = new ControllerCalibration();
            Quaternion corrected = ControllerCalibration.LocalPoseRotation(Quaternion.identity);
            // In output coordinates, rotating +X by +45 around +Y gives (+sqrt(.5),0,-sqrt(.5)).
            Near(correctionFrame.Rotation(corrected) * Vector3.right,
                new Vector3(Mathf.Sqrt(0.5f), 0, -Mathf.Sqrt(0.5f)), "local Y correction sign");
            Quaternion correctedTilt = ControllerCalibration.LocalPoseRotation(tilted);
            Near(correctedTilt * Vector3.forward, tilted * (corrected * Vector3.forward),
                "correction uses local rather than fixed axes");
            Require(correctionFrame.Update(true, true, origin, origin, correctedTilt), "corrected calibration");
            Require(Quaternion.Angle(correctionFrame.Rotation(correctedTilt), Quaternion.identity) < 0.001f,
                "corrected right pose is identity immediately after calibration");
            var leftHold = new ControllerPoseState();
            var rightHold = new ControllerPoseState();
            leftHold.Sample(true, origin, tilted, Vector3.one, Vector3.one, true);
            Require(!leftHold.HasPose && leftHold.LiveValid, "calibration does not latch left pose");
            leftHold.StickPressed = true;
            rightHold.StickPressed = true;
            leftHold.Sample(true, origin, tilted, Vector3.one, Vector3.one, true);
            rightHold.Sample(true, origin, tilted, Vector3.one, Vector3.one, true);
            rightHold.StickPressed = false;
            leftHold.StickPressed = true;
            leftHold.Sample(true, origin + Vector3.right, pose, Vector3.one, Vector3.up);
            rightHold.Sample(true, origin + Vector3.left, pose, Vector3.one, Vector3.up);
            Near(leftHold.Position, origin + Vector3.right, "left click updates left pose");
            Near(rightHold.Position, origin, "right pose independently held");
            Near(rightHold.LivePosition, origin + Vector3.left, "right ghost remains live");
            Require(Quaternion.Angle(rightHold.Rotation, tilted) < 0.001f, "held rotation stable");
            Near(rightHold.Velocity, Vector3.zero, "held velocity zero");
            leftHold.StickPressed = false;
            leftHold.Sample(true, origin + Vector3.up, tilted, Vector3.one, Vector3.one);
            Near(leftHold.Position, origin + Vector3.right, "release freezes last position");
            Require(Quaternion.Angle(leftHold.Rotation, pose) < 0.001f, "release freezes last rotation");
            rightHold.StickPressed = true;
            rightHold.Sample(true, origin + Vector3.up, pose, Vector3.one, Vector3.one);
            Near(rightHold.Position, origin + Vector3.up, "right click resumes independently");
            leftHold.Sample(true, origin, tilted, Vector3.zero, Vector3.zero, true);
            Require(!leftHold.HasPose && leftHold.LiveValid, "recalibration clears stale pose without latching");
            rightHold.Sample(false, Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero);
            Near(rightHold.Position, origin + Vector3.up, "tracking loss preserves last pose");
            Require(!rightHold.Updating && !rightHold.LiveValid, "tracking loss stops updates and live ghost");
            Debug.Log("Controller calibration and independent pose-hold validation passed.");
        }

        private static void Near(Vector3 actual, Vector3 expected, string label)
        {
            Require((actual - expected).sqrMagnitude < 0.000001f, label + $": {actual} != {expected}");
        }
        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Calibration validation failed: " + label);
        }
    }
}
