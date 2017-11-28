using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Random : MonoBehaviour {

    public double ale;
    // Use this for initialization
    void Start() {
    
    double getAle()
    {
            int n = UnityEngine.Random.Range(0, 1);
            int dec = UnityEngine.Random.Range(0,9999);
            string cadena = n.ToString() + "." + dec.ToString();
            double Ale= System.Convert.ToDouble(cadena) ;
        double I1 = 2 * Ale;
        double I2 = I1 / 2;
        ale = I2;
        System.Console.Write(cadena);
        return ale;
    }
} 

	
	// Update is called once per frame
	void Update () {

        double getAle()
        {
            int n = UnityEngine.Random.Range(0, 1);
            int dec = UnityEngine.Random.Range(0, 9999);
            string cadena = n.ToString() + "." + dec.ToString();
            double Ale = System.Convert.ToDouble(cadena);
            double I1 = 2 * Ale;
            double I2 = I1 / 2;
            ale = I2;
            System.Console.Write(cadena);
            return ale;
        }
    }
}
