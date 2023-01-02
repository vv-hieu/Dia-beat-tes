using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public HashSet<Vector2Int> vertices { get; private set; } = new HashSet<Vector2Int>();
    public HashSet<Edge>       edges    { get; private set; } = new HashSet<Edge>();

    public void Clear()
    {
        vertices.Clear();
        edges.Clear();
    }

    public void AddVertex(Vector2Int v)
    {
        vertices.Add(v);
    }

    public void RemoveVertex(Vector2Int v)
    {
        if (vertices.Remove(v))
        {
            List<Edge> removedEdges = new List<Edge>();
            foreach (Edge e in edges)
            {
                if (e.ContainsVertex(v))
                {
                    removedEdges.Add(e);
                }
            }
            foreach (Edge e in removedEdges)
            {
                edges.Remove(e);
            }
        }
    }

    public void AddEdge(Edge edge)
    {
        if (edge.v1 == edge.v2)
        {
            return;
        }

        vertices.Add(edge.v1);
        vertices.Add(edge.v2);

        edges.Add(edge);
    }

    public void AddEdge(Vector2Int v1, Vector2Int v2)
    {
        AddEdge(new Edge(v1, v2));
    }

    public void AddEdge(Vector2Int v1, Vector2Int v2, float length)
    {
        AddEdge(new Edge(v1, v2, length));
    }

    public void RemoveEdge(Edge edge)
    {
        edges.Remove(edge);
    }

    public void RemoveEdge(Vector2Int v1, Vector2Int v2)
    {
        RemoveEdge(new Edge(v1, v2));
    }

    public bool IsConnected(Vector2Int v1, Vector2Int v2)
    {
        List<Vector2Int> scannedVertices = new List<Vector2Int>();
        scannedVertices.Add(v1);
        int i = 0;

        while (i < scannedVertices.Count)
        {
            Vector2Int currentVertex = scannedVertices[i];
            foreach (Edge e in edges)
            {
                if (e.ContainsVertex(currentVertex, out Vector2Int otherVertex) && !scannedVertices.Contains(otherVertex))
                {
                    if (otherVertex == v2)
                    {
                        return true;
                    }
                    scannedVertices.Add(otherVertex);
                }
            }
            ++i;
        }

        return false;
    }

    public Graph MinSpanTree()
    {
        Graph res = new Graph();

        if (vertices.Count <= 1)
        {
            foreach (Vector2Int v in vertices)
            {
                res.AddVertex(v);
            }
            return res;
        }

        List<Edge> sortedEdges = new List<Edge>(edges);
        sortedEdges.Sort(delegate (Edge c1, Edge c2) 
        { 
            return (c1.length < c2.length ? -1 : c1.length > c2.length ? 1 : 0); 
        });

        foreach (Edge e in sortedEdges)
        {
            if (!res.IsConnected(e.v1, e.v2))
            {
                res.AddEdge(e);
                if (res.edges.Count >= vertices.Count - 1)
                {
                    break;
                }
            }
        }

        return res;
    }

    public void Draw(Color vertexColor, Color edgeColor, float z)
    {
        foreach (Edge e in edges)
        {
            Vector3 v1 = new Vector3(e.v1.x, e.v1.y, z);
            Vector3 v2 = new Vector3(e.v2.x, e.v2.y, z);
            Debug.DrawLine(v1, v2, edgeColor, 0.0f);
        }

        Color temp = Gizmos.color;
        Gizmos.color = vertexColor;
        foreach (Vector2Int v in vertices)
        {
            Gizmos.DrawSphere(new Vector3(v.x, v.y, z), 0.3f);
        }
        Gizmos.color = temp;
    }

    public struct Edge
    {
        public Vector2Int v1     { get; private set; }
        public Vector2Int v2     { get; private set; }
        public float      length { get; private set; }

        public override bool Equals(object obj)
        {
            Edge other = (Edge)obj;
            return v1 == other.v1 && v2 == other.v2;
        }

        public override int GetHashCode()
        {
            return new Vector4(v1.x, v1.y, v2.x, v2.y).GetHashCode();
        }

        public Edge(Vector2Int v1, Vector2Int v2)
        {
            if (s_Compare(v1, v2))
            {
                this.v1 = v1;
                this.v2 = v2;
            }
            else
            {
                this.v1 = v2;
                this.v2 = v1;
            }
            length = Vector2.Distance(v1, v2);
        }

        public Edge(Vector2Int v1, Vector2Int v2, float length)
        {
            if (s_Compare(v1, v2))
            {
                this.v1 = v1;
                this.v2 = v2;
            }
            else
            {
                this.v1 = v2;
                this.v2 = v1;
            }
            this.length = length;
        }

        public bool ContainsVertex(Vector2Int v)
        {
            return v == v1 || v == v2;
        }

        public bool ContainsVertex(Vector2Int v, out Vector2Int other)
        {
            if (v == v1)
            {
                other = v2;
                return true;
            }
            if (v == v2)
            {
                other = v1;
                return true;
            }
            other = Vector2Int.zero;
            return false;
        }

        private static bool s_Compare(Vector2Int a, Vector2Int b)
        {
            return (a.x < b.x) || (a.x == b.x && a.y <= b.y);
        }
    }
}
