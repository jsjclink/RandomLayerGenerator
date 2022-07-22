using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//hierarchy : 1 ~ n, floor : 0 ~ m, layer_idx : 0 ~ 4, map_idx : 0 ~ l
public class MapInfo
{
	public string id;
	public int hierarchy_idx;
	public int floor;     //´øÀü¿¡¼­ ÁøÂ¥ Ãþ
	public int layer_idx; //°èÃþ¿¡¼­ »ó´ëÀûÀÎ Ãþ
	public int map_idx;
	public List<MapInfo> connected_map_list;

	public MapInfo(int hierarchy_idx, int layer_idx, int map_idx)
	{
		this.floor = (hierarchy_idx - 1) * 5 + layer_idx;

		this.hierarchy_idx = hierarchy_idx;
		this.layer_idx = layer_idx;
		this.map_idx = map_idx;

		this.id = this.floor + "_" + map_idx;
		
		connected_map_list = new List<MapInfo>();
	}
}

class Hierarchy
{
	private int hierarchy_idx;
	public List<MapInfo>[] mapInfos_of_layer = new List<MapInfo>[5]; //Ãþ¸¶´Ù ¸Ê Á¤º¸ ÀúÀå

	public Hierarchy(int hierarchy_idx, int difficulty)
	{
		this.hierarchy_idx = hierarchy_idx;
		for (int i = 0; i < 5; i++)
		{
			mapInfos_of_layer[i] = new List<MapInfo>();
			if (i == 0)
			{
				mapInfos_of_layer[i].Add(new MapInfo(hierarchy_idx, 0, 0));
			}
			else
			{
				int mapnum = i * hierarchy_idx + Random.Range(-i + 1, i + difficulty + 1);
				if (mapnum > 5) mapnum = 5;
				for (int j = 0; j < mapnum; j++)
				{
					mapInfos_of_layer[i].Add(new MapInfo(hierarchy_idx, i, j));
				}
			}
		}
		//0-0ÀÌ¶û 1-0 ¿¬°á
		mapInfos_of_layer[0][0].connected_map_list.Add(mapInfos_of_layer[1][0]);
		mapInfos_of_layer[1][0].connected_map_list.Add(mapInfos_of_layer[0][0]);
	}
	public void Create()
	{
		List<MapInfo> unconnected_list = new List<MapInfo>();

		while((unconnected_list = FindUnConnected()).Count > 0)
        {
			string output = "unconnected : ";
			foreach(MapInfo m in unconnected_list)
            {
				output += m.id + ", "; 
            }
			Debug.Log(output);

			int rand_idx = Random.Range(0, unconnected_list.Count);
			int from_layer_idx = unconnected_list[rand_idx].layer_idx;
			int from_map_idx = unconnected_list[rand_idx].map_idx;
			MapInfo from = mapInfos_of_layer[from_layer_idx][from_map_idx];

			List<MapInfo> choose_list = new List<MapInfo>();

			int min_find_layer = from_layer_idx - 1;
			if (min_find_layer < 1) min_find_layer = 1;

			int max_find_layer = from_layer_idx + 1;
			if (max_find_layer > 4) max_find_layer = 4;

			for (int i = min_find_layer; i <= max_find_layer; i++)
			{
				foreach (MapInfo cur in mapInfos_of_layer[i])
				{
					if (cur.id != from.id) choose_list.Add(cur);
				}
			}

			if (from.connected_map_list.Count == choose_list.Count) continue; //prevent infinite loop

			while (true)
			{
				int idx = Random.Range(0, choose_list.Count);
				MapInfo to = choose_list[idx];
				if (!from.connected_map_list.Contains(to))
				{
					from.connected_map_list.Add(to);
					to.connected_map_list.Add(from);
					break;
				}
			}
		}
	}

	public List<MapInfo> FindUnConnected()
    {
		Dictionary<string, bool> visited = new Dictionary<string, bool>();
		for (int i = 0; i < 5; i++)
		{
			foreach (MapInfo cur in mapInfos_of_layer[i])
			{
				visited[cur.id] = false;
			}
		}
		visited[mapInfos_of_layer[0][0].id] = true;

		Queue<MapInfo> queue = new Queue<MapInfo>();
		queue.Enqueue(mapInfos_of_layer[0][0]);

		while (queue.Count > 0)
		{
			MapInfo cur = queue.Dequeue();
			foreach (MapInfo m in cur.connected_map_list)
			{
				if (!visited[m.id])
				{
					visited[m.id] = true;
					queue.Enqueue(m);
				}
			}
		}

		List<MapInfo> unconnected_list = new List<MapInfo>();
		for(int i = 0; i < 5; i++)
        {
			foreach (MapInfo m in mapInfos_of_layer[i])
            {
				if (visited[m.id] == false) unconnected_list.Add(m);
            }
		}
		return unconnected_list;
	}

	public void Connect(Hierarchy hierarchy)
	{
		int map_idx = Random.Range(0, mapInfos_of_layer[4].Count);
		MapInfo from = mapInfos_of_layer[4][map_idx];

		from.connected_map_list.Add(hierarchy.mapInfos_of_layer[0][0]);
		hierarchy.mapInfos_of_layer[0][0].connected_map_list.Add(from);
	}
}


public class RandomLayerGenerator : MonoBehaviour
{
	[SerializeField]
	GameObject square;
	void Start()
	{
		Hierarchy hierarchy1 = new Hierarchy(1, 3);
		Hierarchy hierarchy2 = new Hierarchy(2, 3);
		hierarchy1.Create();
		hierarchy2.Create();
		hierarchy1.Connect(hierarchy2);

		for(int i = 0; i < 5; i++)
        {
			foreach(MapInfo cur in hierarchy1.mapInfos_of_layer[i])
            {
				Vector3 from_pos = new Vector3(cur.map_idx, 10 - cur.layer_idx - (5*(cur.hierarchy_idx - 1)), 0);
				Instantiate(square, from_pos, Quaternion.identity);
				
				foreach(MapInfo m in cur.connected_map_list)
                {
					Vector3 to_pos = new Vector3(m.map_idx, 10 - m.layer_idx - (5*(m.hierarchy_idx-1)), 0);

					LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
					lineRenderer.startColor = Color.black;
					lineRenderer.endColor = Color.black;
					lineRenderer.startWidth = 0.01f;
					lineRenderer.endWidth = 0.01f;
					lineRenderer.positionCount = 2;
					lineRenderer.useWorldSpace = true;

					//For drawing line in the world space, provide the x,y,z values
					lineRenderer.SetPosition(0, from_pos); //x,y and z position of the starting point of the line
					lineRenderer.SetPosition(1, to_pos); //x,y and z position of the end point of the line


				}
			}
        }

		for (int i = 0; i < 5; i++)
		{
			foreach (MapInfo cur in hierarchy2.mapInfos_of_layer[i])
			{
				Vector3 from_pos = new Vector3(cur.map_idx, 10 - cur.layer_idx - (5 * (cur.hierarchy_idx - 1)), 0);
				Instantiate(square, from_pos, Quaternion.identity);

				foreach (MapInfo m in cur.connected_map_list)
				{
					Vector3 to_pos = new Vector3(m.map_idx, 10 - m.layer_idx - (5 * (m.hierarchy_idx - 1)), 0);

					LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
					lineRenderer.startColor = Color.black;
					lineRenderer.endColor = Color.black;
					lineRenderer.startWidth = 0.01f;
					lineRenderer.endWidth = 0.01f;
					lineRenderer.positionCount = 2;
					lineRenderer.useWorldSpace = true;

					//For drawing line in the world space, provide the x,y,z values
					lineRenderer.SetPosition(0, from_pos); //x,y and z position of the starting point of the line
					lineRenderer.SetPosition(1, to_pos); //x,y and z position of the end point of the line


				}
			}
		}


		/*
		Debug.Log("hierarchy1");

		for(int i = 0; i < 5; i++)
        {
			string output = "floor" + i + " : ";
			foreach(MapInfo cur in hierarchy1.mapInfos_of_layer[i])
            {
				output += cur.id + ", ";
            }
			Debug.Log(output);
        }

		for (int i = 0; i < 5; i++)
		{
			foreach (MapInfo cur in hierarchy1.mapInfos_of_layer[i])
			{
				string output = cur.floor + "_" + cur.map_idx + ", CONNECTED TO : ";
				foreach (MapInfo con in cur.connected_map_list)
				{
					output += con.floor + "_" + con.map_idx + ", ";
				}
				Debug.Log(output);
			}
		}

		Debug.Log("hierarchy2");

		for (int i = 0; i < 5; i++)
		{
			foreach (MapInfo cur in hierarchy2.mapInfos_of_layer[i])
			{
				string output = cur.floor + "_" + cur.map_idx + ", CONNECTED TO : ";
				foreach (MapInfo con in cur.connected_map_list)
				{
					output += con.floor + "_" + con.map_idx + ", ";
				}
				Debug.Log(output);
			}
		}*/
	}
}