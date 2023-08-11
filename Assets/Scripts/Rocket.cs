using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Utils;

public class Rocket : MonoBehaviour {
    [SerializeField]
    Transform rocket;

    [SerializeField]
    float baseRocketSpeed = 10f;

    [SerializeField]
    float boostMultiplier = 1.5f;

    [SerializeField]
    float boostTime = 1f;

    [CanBeNull]
    Planet planetWithActingGravity;
    [CanBeNull]
    Planet lastPlanetWithActingGravity;

    bool rocketLaunched;
    int steeringRotation = -1; // -1 right, 1 left
    float boostSpeed;

    float distancePassed;
    float rocketSpeed;

    Vector2 startPosition;


    void Start() {
        InputController.Instance.Interact += OnInteract;
        rocketSpeed = baseRocketSpeed;
        startPosition = rocket.position;
    }


    void OnDestroy() {
        InputController.Instance.Interact -= OnInteract;
    }

    void OnInteract() {
        if (rocketLaunched) {
            Boost();
        }
        else {
            LaunchRocket();
        }
    }

    void LaunchRocket() {
        if (!rocketLaunched) {
            rocketLaunched = true;
        }
    }

    void Boost() {
        StartCoroutine(LeavePlanetGravity());
        StartCoroutine(BoostRocket());
    }


    IEnumerator BoostRocket() {
        float targetSpeed = rocketSpeed * boostMultiplier;
        float t = 0;

        while (t < boostTime) {
            boostSpeed = GetBoostSpeed(t, targetSpeed);
            t += Time.deltaTime;
            yield return null;
        }
        boostSpeed = 0;
    }


    float GetBoostSpeed(float t, float maxSpeed) {
        float timeToAccelerate = boostTime * 0.2f;
        float timeToDecelerate = boostTime * 0.8f;

        if (t < timeToAccelerate) {
            return (t / timeToAccelerate) * maxSpeed;
        }
        if (t > boostTime - timeToDecelerate) {
            float t2 = t - (boostTime - timeToDecelerate);
            return maxSpeed - (t2 / timeToDecelerate) * maxSpeed;
        }
        return maxSpeed;
    }

    IEnumerator LeavePlanetGravity() {
        lastPlanetWithActingGravity = planetWithActingGravity;
        planetWithActingGravity = null;
        transform.parent = null;

        yield return new WaitForSeconds(1f);
        lastPlanetWithActingGravity = null;
    }

    public void BlowUp() {
        Debug.Log("BOOM!!!");
        Destroy(this);
    }

    public void Land(Vector2 landingPos) {
        rocketSpeed = 0;
        boostSpeed = 0;
    }

    void Update() {
        if (rocketLaunched) {
            Debug.Log($"{rocketSpeed} {distancePassed}");
            float finalSpeed = rocketSpeed + boostSpeed;

            if (!planetWithActingGravity) {
                planetWithActingGravity = FindPlanetWithActingGravity();

                if (planetWithActingGravity) {
                    // var planetPos = planetWithActingGravity.transform.position;
                    // Vector2 dirToRocketFromPlanet = (rocket.position - planetPos).normalized;
                    // steeringRotation = MathFG.WedgeProduct(dirToRocketFromPlanet, rocket.up) < 0 ? -1 : 1;
                }
            }

            if (planetWithActingGravity) {
                transform.parent = planetWithActingGravity.transform;

                var planetPos = planetWithActingGravity.transform.position;
                Vector2 dirToRocketFromPlanet = (rocket.position - planetPos).normalized;
                float angleSpeedRad = (finalSpeed / planetWithActingGravity.GravityRadius) * steeringRotation * planetWithActingGravity.GravityAngularSpeed;
                Vector2 rotatedDir = Quaternion.Euler(0, 0, angleSpeedRad * Time.deltaTime * Mathf.Rad2Deg) * dirToRocketFromPlanet;

                Vector3 nextPosition = (Vector3)(rotatedDir * planetWithActingGravity.GravityRadius) + planetPos;
                CalculateDistancePassed();

                rocket.right = rotatedDir * steeringRotation;
                rocket.position = nextPosition;
            }
            else {
                Vector3 nextPosition = rocket.position + rocket.up * (Time.deltaTime * finalSpeed);
                CalculateDistancePassed();
                rocket.position = nextPosition;
            }

            // CalculateSpeed();
        }

    }

    void CalculateSpeed() {
        rocketSpeed = baseRocketSpeed + distancePassed * 0.01f;
    }

    void CalculateDistancePassed() {
        float xPassed = rocket.position.x - startPosition.x;
        if (xPassed > distancePassed) {
            distancePassed = xPassed;
        }
    }


    [CanBeNull]
    Planet FindPlanetWithActingGravity() {
        foreach (var planet in PlanetController.Instance.Planets) {
            if (!ReferenceEquals(lastPlanetWithActingGravity, planet) && planet.InsideGravity(rocket.position)) {
                return planet;
            }
        }
        return null;
    }

}