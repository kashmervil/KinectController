module SkeletonProcessing
open System
open Microsoft.Kinect

let mutable minHeight = 20.
let mutable maxHeight = 400.
let eps = 10

let inline trackingId (x: Skeleton) = x.TrackingId

let ToPoint(s,c) = 
    CoordinateMapper(KinectSensor.KinectSensors.[0]).MapSkeletonPointToColorPoint(s, c)

let inline isJointInferred (joint: Joint) = joint.TrackingState = JointTrackingState.Inferred
let inline isSkeletonTracked (skeleton : Skeleton) = skeleton.TrackingState = SkeletonTrackingState.Tracked

let getPoints (skeleton: Skeleton) =
    let l = skeleton.Joints.[JointType.HandLeft]
    let r = skeleton.Joints.[JointType.HandRight]
    if (isJointInferred l) && (isJointInferred r) 
    then None
    else
        let cf = ColorImageFormat.RgbResolution640x480Fps30
        let leftHand = ToPoint(l.Position, cf)
        let rightHand = ToPoint(r.Position, cf)
        let tmp = (leftHand.X, rightHand.X), (leftHand.Y, rightHand.Y)
        Some tmp

let tupleAverage (source: System.Collections.Generic.IList<int*int>) = 
    let mutable maxL = 0
    let mutable maxR = 0
    for (l, r) in source do
        maxL <- Math.Max(maxL, abs l)
        maxR <- Math.Max(maxR, abs r)
    (maxL, maxR)

let flappingScale (l,r) =
    let helper x = 
        if x < 2 * eps then 0
        elif x > 100 then 100
        else x
    helper <| abs (2 * l), helper <| abs (2 * r)

let floatingFiltering (l,r) =
    let lim x = 
        let x' = double x
        x' > minHeight && x' < maxHeight in (lim l) && (lim r)

let floatingScale (l,r) = 
    let helper x' =
        let x = float x'
        100. - ((x - minHeight) * 100. / (maxHeight - minHeight)) 
    helper l, helper r

let toFlappingHands hands =
    hands 
    |> ObservableHelpers.TakeDerivative
    |> ObservableHelpers.Buffer 10 1
    |> Observable.map tupleAverage
    |> Observable.map flappingScale

let toFloatingHands hands = 
    hands
    |> ObservableHelpers.Buffer 20 2
    |> Observable.map tupleAverage  
    |> Observable.filter floatingFiltering
    |> Observable.map floatingScale