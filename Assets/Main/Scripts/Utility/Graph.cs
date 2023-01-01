using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public HashSet<Vector2Int> vertices { get; private set; } = new HashSet<Vector2Int>();
    public HashSet<KeyValuePair<Vector2Int, Vector2Int>> edges { get; private set; } = new HashSet<KeyValuePair<Vector2Int, Vector2Int>>();

    public void AddEdge(Vector2Int v0, Vector2Int v1)
    {
        vertices.Add(v0);
        vertices.Add(v1);

        if (v0.x > v1.x || (v0.x == v1.x && v0.y > v1.y))
        {
            Vector2Int temp = v0;
            v0 = v1;
            v1 = temp;
        }
        else if (v0.x == v1.x && v0.y == v1.y)
        {
            return;
        }

        edges.Add(new KeyValuePair<Vector2Int, Vector2Int>(v0, v1));
    }

    public void RemoveEdge(Vector2Int v0, Vector2Int v1)
    {
        if (v0.x > v1.x || (v0.x == v1.x && v0.y > v1.y))
        {
            Vector2Int temp = v0;
            v0 = v1;
            v1 = temp;
        }
        else if (v0.x == v1.x && v0.y == v1.y)
        {
            return;
        }

        if (edges.Remove(new KeyValuePair<Vector2Int, Vector2Int>(v0, v1)))
        {
            bool b0 = false;
            bool b1 = false;
            foreach (KeyValuePair<Vector2Int, Vector2Int> edge in edges)
            {
                if (edge.Key == v0 || edge.Value == v0)
                {
                    b0 = true;
                }
                if (edge.Key == v1 || edge.Value == v1)
                {
                    b1 = true;
                }
                if (b0 && b1)
                {
                    break;
                }
            }
            if (!b0)
            {
                vertices.Remove(v0);
            }
            if (!b1)
            {
                vertices.Remove(v1);
            }
        }
    }

    public void SplitEdge(Vector2Int v0, Vector2Int v1, Vector2Int v)
    {
        if (v0.x > v1.x || (v0.x == v1.x && v0.y > v1.y))
        {
            Vector2Int temp = v0;
            v0 = v1;
            v1 = temp;
        }
        else if (v0.x == v1.x && v0.y == v1.y)
        {
            return;
        }
        else if (v.x == v0.x && v.y == v0.y)
        {
            return;
        }
        else if (v.x == v1.x && v.y == v1.y)
        {
            return;
        }

        if (edges.Remove(new KeyValuePair<Vector2Int, Vector2Int>(v0, v1)))
        {
            AddEdge(v, v0);
            AddEdge(v, v1);
        }
    }

    public void RemoveVertex(Vector2Int v)
    {
        if (vertices.Remove(v))
        {
            HashSet<KeyValuePair<Vector2Int, Vector2Int>> newEdges = new HashSet<KeyValuePair<Vector2Int, Vector2Int>>();
            foreach (KeyValuePair<Vector2Int, Vector2Int> edge in edges)
            {
                if (edge.Key != v && edge.Value != v)
                {
                    newEdges.Add(edge);
                }
            }
            edges = newEdges;
        }

        HashSet<Vector2Int> newVertices = new HashSet<Vector2Int>();
        foreach (Vector2Int vertex in vertices)
        {
            bool hanging = true;
            foreach (KeyValuePair<Vector2Int, Vector2Int> edge in edges)
            {
                if (edge.Key == vertex || edge.Value == vertex)
                {
                    hanging = false;
                    break;
                }
            }
            if (!hanging)
            {
                newVertices.Add(vertex);
            }
        }
        vertices = newVertices;
    }

    public void AddSubgraph(Graph subgraph)
    {
        foreach (KeyValuePair<Vector2Int, Vector2Int> edge in subgraph.edges)
        {
            AddEdge(edge.Key, edge.Value);
        }
    }

    public List<Graph> ConnectedComponents()
    {
        List<Graph> res = new List<Graph>();

        HashSet<Vector2Int> scanned = new HashSet<Vector2Int>();

        foreach (Vector2Int vertex in vertices)
        {
            if (!scanned.Contains(vertex))
            {
                Graph component = new Graph();
                scanned.Add(vertex);

                HashSet<Vector2Int> componentVertices = new HashSet<Vector2Int>();
                componentVertices.Add(vertex);

                while (true)
                {
                    bool complete = true;

                    HashSet<Vector2Int> additionalComponentVertices = new HashSet<Vector2Int>();
                    foreach (Vector2Int componentVertex in componentVertices)
                    {
                        foreach (KeyValuePair<Vector2Int, Vector2Int> edge in edges)
                        {
                            if (edge.Key == componentVertex || edge.Value == componentVertex)
                            {
                                additionalComponentVertices.Add(edge.Key);
                                additionalComponentVertices.Add(edge.Value);
                            }
                        }
                    }
                    foreach (Vector2Int additionalComponentVertex in additionalComponentVertices)
                    {
                        scanned.Add(additionalComponentVertex);
                        if (componentVertices.Add(additionalComponentVertex))
                        {
                            complete = false;
                        }
                    }

                    if (complete)
                    {
                        foreach (Vector2Int componentVertex in componentVertices)
                        {
                            foreach (KeyValuePair<Vector2Int, Vector2Int> edge in edges)
                            {
                                if (edge.Key == componentVertex || edge.Value == componentVertex)
                                {
                                    component.AddEdge(edge.Key, edge.Value);
                                }
                            }
                        }
                        break;
                    }
                }

                res.Add(component);
            }
        }

        return res;
    }

    public bool IsConnected(Vector2Int a, Vector2Int b)
    {
        List<Vector2Int> connectedVertices = new List<Vector2Int>();
        connectedVertices.Add(a);
        int i = 0;
        while (i < connectedVertices.Count)
        {
            foreach (KeyValuePair<Vector2Int, Vector2Int> edge in edges)
            {
                if (edge.Key == connectedVertices[i])
                {
                    if (edge.Value == b)
                    {
                        return true;
                    }
                    if (!connectedVertices.Contains(edge.Value))
                    {
                        connectedVertices.Add(edge.Value);
                    }
                }
                else if (edge.Value == connectedVertices[i])
                {
                    if (edge.Key == b)
                    {
                        return true;
                    }
                    if (!connectedVertices.Contains(edge.Key))
                    {
                        connectedVertices.Add(edge.Key);
                    }
                }
            }
            ++i;
        }
        return false;
    }
}
