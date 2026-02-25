// UMD IMDM290 
// Instructor: Myungin Lee
// This tutorial introduce a way to draw shapes and align them in a circle with colors.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateShapes : MonoBehaviour
{
    public GameObject prefab; // Prefab for the shapes
    public GameObject parent;
    public int resolution = 10;  
    public float speed = 1f;

    GameObject[] shapes;
    Color[][] colors;
    Renderer[] shapesRenderers;
    
    System.Random rand = new System.Random();
                

    void Start(){
        shapes = new GameObject[resolution];
        shapesRenderers = new Renderer[resolution];
        colors = new Color[resolution][];

           for (int i = 0; i < resolution; i++){
                colors[i] = new Color[4];
                shapes[i] = GameObject.Instantiate(prefab);
                shapes[i].transform.parent = parent.transform;

                colors[i][0] = new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                colors[i][1]  = new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble() , (float)rand.NextDouble());
                colors[i][2]  = new Color((float)rand.NextDouble(), (float)rand.NextDouble() , (float)rand.NextDouble() ,(float)rand.NextDouble());
                colors[i][3]  = new Color((float)rand.NextDouble() , (float)rand.NextDouble() , (float)rand.NextDouble() , (float)rand.NextDouble());


                Vector3 position = new Vector3(5f*((colors[i][0]).r - 0.5f) , 5f*((colors[i][0]).g  - 0.5f),  5*((colors[i][0]).b - 0.5f));
                Quaternion rotation = Quaternion.Euler(720 * ((colors[i][1]).r) - 360, 720 * ((colors[i][1]).g) - 360, (720 * ((colors[i][1]).b) - 360 ));
                Vector3 scale = new  Vector3(.05f * (colors[i][3].r + .5f) , .05f * (colors[i][3].g + .5f),  .05f * (colors[i][3].b + 0.5f));

                shapes[i].transform.position = position;
                shapes[i].transform.rotation = rotation;
                shapes[i].transform.localScale = scale;

                shapesRenderers[i] = shapes[i].GetComponent<Renderer>();
                shapesRenderers[i].material.color = colors[i][2];
           }
    }

    void Update(){
        for (int i = 0; i < resolution; i++){

            Color.RGBToHSV(colors[i][0], out float h, out float s, out float v);
            Color.RGBToHSV(colors[i][1], out float h2, out float s2, out float v2);
            Color.RGBToHSV(colors[i][2], out float h3, out float s3, out float v3);
            Color.RGBToHSV(colors[i][3], out float h4, out float s4, out float v4);



            float newH1 = (h + Time.deltaTime * speed) % 1f;
            float newH2 = (h2 + Time.deltaTime * speed) % 1f;
            float newH3 = (h3 + Time.deltaTime * speed) % 1f;
            float newH4 = (h4 + Time.deltaTime * speed) % 1f;

            colors[i][0] = Color.HSVToRGB(newH1,s,v);
            colors[i][1] = Color.HSVToRGB(newH2, s2, v2);
            colors[i][2] = Color.HSVToRGB(newH3,  s3, v3);
            colors[i][3] = Color.HSVToRGB(newH4, s4, v4);


            Vector3 position = new Vector3(5*((colors[i][0]).r - 0.5f) , 5*((colors[i][0]).g - 0.5f),  5*((colors[i][0]).b - 0.5f)); ;
            Quaternion rotation = Quaternion.Euler(720 * ((colors[i][1]).r ) - 360, 720 * ((colors[i][1]).g ) - 360, (720 * ((colors[i][1]).b ) - 360 ));
            Vector3 scale = new  Vector3(0.5f * (colors[i][3].r + 0.5f), 0.5f*(colors[i][3].g + 0.5f), 0.5f*(colors[i][3]).b + .5f);

            shapes[i].transform.position = position;
            shapes[i].transform.rotation = rotation;
            shapes[i].transform.localScale = scale;

            shapesRenderers[i].material.color = colors[i][2];
        }
    }
}