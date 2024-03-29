using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class WaveGenerator : MonoBehaviour {

	int widthVerts = 100;		//The "width" of the base plane in number of vertices.
	int lengthVerts = 100;		//The "length" of the base plane in number of vertices.
	float width = 200;			//Base plane width.
	float length = 200;			//Base plane length.

	Vector3[] vertices;			//The base plane's vertices.
	int[] triangles;			//The base plane's triangles.

	public float noiseStrength = 1.2f;			//Global wave noise strength.

	public float dirDiff = 15f;					//Difference in wave travel direction.

	//Used to store the parameters of a wave.
	[System.Serializable]public class Wave
	{
		[Range(0,360)]public float degree = 0;			//Direction of travel, in degrees.
		public Vector2 direction;						//Direction of travel, as a vector.
		[Range(1,200)]public float wavelength;			//Wavelength.
		[Range(0,5)]public float amplitude;				//Amplitude.
		public float speed;								//Speed.
		[Range(0,1)] public float q;					//Q-value.
	}

	//Create two waves.
	public Wave wave = new Wave();
	public Wave wave2 = new Wave();

	//The "base position" of each vertex.
	private Vector3[] baseHeight;

	// Use this for initialization
	void Start () {
		//Set starting parameters for the waves.
		//Using slightly different parameters makes for more interesting wave patterns.
		wave.amplitude = 2.31f;
		wave.wavelength = 50.0f;
		wave.speed = 5.0f;
		wave.direction = new Vector2(1,0);
		wave.degree = 0;
		wave.q = 2.1f;

		wave2.amplitude = 1.0f;
		wave2.wavelength = 20.0f;
		wave2.speed = 5.0f;
		wave2.direction = new Vector2(1,0);
		wave2.degree = wave.degree + dirDiff;
		wave2.q = 2.1f;

		//Set global wave parameters.
		noiseStrength = 1.2f;
		dirDiff = 15f;

		createBasePlane ();
	}





	// Update is called once per frame
	void Update () {
		//Get the directional vector from the degree.
		wave.direction.x = Mathf.Cos (wave.degree * Mathf.Deg2Rad);
		wave.direction.y = Mathf.Sin (wave.degree * Mathf.Deg2Rad);

		wave2.direction.x = Mathf.Cos ((wave.degree + dirDiff) * Mathf.Deg2Rad);
		wave2.direction.y = Mathf.Sin ((wave.degree + dirDiff) * Mathf.Deg2Rad);

		//Calculate the wave speed.
		wave.speed = Mathf.Sqrt (9.8f * wave.wavelength / (2 * Mathf.PI));

		//Make sure we have a MeshFilter.
		if (GetComponent<MeshFilter>() != null) {
			Mesh mesh = GetComponent<MeshFilter> ().mesh;

			//Make sure we have the vertices base positions.
			if (baseHeight == null) {
				baseHeight = mesh.vertices;
			}
			
			Vector3[] vertices = new Vector3[baseHeight.Length];
			for (int i = 0; i < vertices.Length; i++) {
				
				//Get the vertex base position.
				Vector3 vertex = baseHeight [i];

				//Create a positional (horizontal) vector for the vertex.
				Vector2 position = new Vector2 (baseHeight [i].x, baseHeight [i].z);

				//Add wave noise.
				vertex.y += Mathf.PerlinNoise (baseHeight [i].x + 0.5f, baseHeight [i].z * 0.5f) * noiseStrength;

				//Calculate new position of vertex and overwrite old vertex.
				//Note that the Gerstner wave function use a right-handed coordinate system.
				Vector3 temp = Gerstner(wave, position);
				vertex.x += temp.x;
				vertex.z += temp.y;
				vertex.y += temp.z;
				vertices [i] = vertex;
			}

			//Write vertices to the mesh and recalculate normal vectors.
			mesh.vertices = vertices;
			mesh.RecalculateNormals ();
		}
	}





	//Returns the 3D position of a vertex according to the Gerstner wave function.
	//Note that this function use a right-handed coordinate system (z as the vertical direction while Unity use y).
	Vector3 Gerstner(Wave wave, Vector2 position) {
		//Calculate the waves frequency and phase constant.
		float frequency = 2 * Mathf.PI / wave.wavelength;
		float frequency2 = 2 * Mathf.PI / wave2.wavelength;
		float phaseconstant = wave.speed * 2 * Mathf.PI / wave.wavelength;
		float phaseconstant2 = wave2.speed * 2 * Mathf.PI / wave2.wavelength;

		float x = position.x + ((wave.q * wave.amplitude * wave.direction.x * Mathf.Cos (frequency * Vector2.Dot (wave.direction, position) + phaseconstant * Time.time))
		          + (wave2.q * wave2.amplitude * wave2.direction.x * Mathf.Cos (frequency2 * Vector2.Dot (wave2.direction, position) + phaseconstant2 * Time.time)));

		float y = position.y + ((wave.q * wave.amplitude * wave.direction.y * Mathf.Cos (frequency * Vector2.Dot (wave.direction, position) + phaseconstant * Time.time))
		           + (wave2.q * wave2.amplitude * wave2.direction.y * Mathf.Cos (frequency2 * Vector2.Dot (wave2.direction, position) + phaseconstant2 * Time.time)));

		float z = (wave.amplitude * Mathf.Sin (frequency * Vector2.Dot (wave.direction, position) + phaseconstant * Time.time))
		          + (wave2.amplitude * Mathf.Sin (frequency2 * Vector2.Dot (wave2.direction, position) + phaseconstant2 * Time.time));

		return new Vector3(x,y,z);
	}






	/*
	 * Adds the base plane to the game object.
	 */
	void createBasePlane() {
		//The total number of vertices and triangles are dependent on the number of vertices in the "width" and "length" directions.
		vertices = new Vector3[widthVerts * lengthVerts];
		triangles = new int[(widthVerts - 1) * (lengthVerts - 1) * 2 * 3];
		Debug.Log ("vertices " + widthVerts * lengthVerts);
		Debug.Log ("Triangles " + (widthVerts - 1) * (lengthVerts - 1) * 2 * 3);

		grid(vertices, triangles);

		Mesh mesh = new Mesh();

		mesh.vertices = vertices;
		mesh.triangles = triangles;

		MeshFilter meshFilter = GetComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		mesh.RecalculateNormals();

		//Calculate the uvs.
		Vector2[] uvs = new Vector2[vertices.Length];
		for (int i=0; i < uvs.Length; i++) {
			uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
		}
		mesh.uv = uvs;

	}





	//Used to show the position of each vertex.
	void OnDrawGizmos() {
		Gizmos.color = Color.white;
		if (vertices != null) {
			for (int i = 0; i < vertices.Length; i++) {
				Gizmos.DrawSphere (vertices[i], 0.1f);
			}
		}
	}





	/*
	 * Create vertices and triangles in the horizontal plane.
	 * Vertices are placed in a grid.
	 */
	void grid(Vector3[] vertices, int[] triangles) {
		int vi = 0;									//Vertex index.
		float lengthS = length/lengthVerts;			//The distance between vertices in the length direction.
		float widthS = width/widthVerts;			//The distance between vertices in the width direction.

		//Create the vertices.
		for (int i = 0; i < lengthVerts; i++) {
			for (int j = 0; j < widthVerts; j++) {
				Vector3 vertex = new Vector3 (j*widthS, 0, i*lengthS);
				vertices[vi] = vertex;
				vi++;
			}
		}

		//Create two trinagles from each "sub-square" of the grid.
		//llv = lower left vertex
		//lrv = lower right vertex
		//ulv = upper left vertex
		//urv = upper right vertex
		int ti = 0;											//Triangle index.
		for (int i = 0; i < lengthVerts - 1; i++) {
			for (int j = 0; j < widthVerts - 1; j++) {
				int llv = (widthVerts * i) + j;
				int lrv = llv + 1;
				int ulv = llv + widthVerts;
				int urv = ulv + 1;

				//Create the first triangle.
				triangles[ti] = llv;
				triangles[ti + 1] = ulv;
				triangles[ti + 2] = lrv;

				ti += 3;

				//Create the second triangle.
				triangles[ti] = lrv;
				triangles[ti + 1] = ulv;
				triangles[ti + 2] = urv;

				ti += 3;
			}
		}
			
	}





	//Set common parameters.
	public void setdirection(float d) {
		this.wave.degree = d;
	}

	public void setnoise(float n) {
		this.noiseStrength = n;
	}

	public void setq(float q) {
		this.wave.q = q;
		this.wave2.q = q;
	}

	public void setSpread(float spread) {
		this.dirDiff = spread;
	}

	//Set individual wave parameters.
	public void setwavelength(float wavelength) {
		this.wave.wavelength = wavelength;
	}

	public void setamplitude(float a) {
		this.wave.amplitude = a;
	}

	public void setwavelength2(float wavelength) {
		this.wave2.wavelength = wavelength;
	}

	public void setamplitude2(float a) {
		this.wave2.amplitude = a;
	}






}
