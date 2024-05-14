using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node
{
    public Tile[] board = new Tile[Constants.NumTiles];
    public Node parent;
    public List<Node> childList = new List<Node>();
    public int type;//Constants.MIN o Constants.MAX
    public double utility;
    public double alfa;
    public double beta;

    public Node(Tile[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            this.board[i] = new Tile();
            this.board[i].value = tiles[i].value;
        }

    }    

}

public class Player : MonoBehaviour
{
    public int turn;    
    private BoardManager boardManager;

    void Start()
    {
        boardManager = GameObject.FindGameObjectWithTag("BoardManager").GetComponent<BoardManager>();
    }

    /*
     * Entrada: Dado un tablero
     * Salida: Posición donde mueve  
     */
    

    public int SelectTile(Tile[] board)
    {
    Node root = new Node(board);
    root.type = Constants.MAX; // El nodo raíz es un nodo MAX

    GenerateMinimaxTree(root, 1, double.MinValue, double.MaxValue);

    // Obtener la lista de movimientos seleccionables después de generar el árbol
    List<int> selectableTiles = boardManager.FindSelectableTiles(board, turn);

    if (selectableTiles.Count == 0)
    {
        Debug.LogError("No hay movimientos disponibles.");
        return -1; // Retorna -1 o maneja como consideres apropiado cuando no hay movimientos disponibles
    }

    int bestMoveIndex = SelectBestMove(root);
    if (bestMoveIndex >= 0 && bestMoveIndex < selectableTiles.Count)
    {
        return selectableTiles[bestMoveIndex];
    }
    else
    {
        Debug.LogError("Índice de mejor movimiento fuera de rango.");
        return selectableTiles[0]; // Retorna un movimiento seguro en caso de error
    }
}


    void GenerateMinimaxTree(Node node, int currentDepth, double alpha, double beta)
    {
        if (currentDepth >= 4) return; // Detener a profundidad 4

        bool isMaximizingPlayer = (node.type == Constants.MAX);
        List<int> nextMoves = boardManager.FindSelectableTiles(node.board, turn);

        foreach (int move in nextMoves)
        {
            Node childNode = new Node(node.board);
            boardManager.Move(childNode.board, move, turn);
            childNode.type = isMaximizingPlayer ? Constants.MIN : Constants.MAX;
            node.childList.Add(childNode);

            // Recursión para generar el siguiente nivel
            GenerateMinimaxTree(childNode, currentDepth + 1, alpha, beta);

            // Poda Alfa-Beta
            if (isMaximizingPlayer)
            {
                alpha = Mathf.Max((float)alpha, (float)childNode.utility);
                if (beta <= alpha)
                    break; // β corte
            }
            else
            {
                beta = Mathf.Min((float)beta, (float)childNode.utility);
                if (beta <= alpha)
                    break; // α corte
            }
        }

        // Establecer la utilidad del nodo basada en el valor alfa o beta
        node.utility = isMaximizingPlayer ? alpha : beta;
    }

    void EvaluateAndPropagateUtility(Node node)
    {
        if (node.childList.Count == 0)
        {
            node.utility = EvaluateBoard(node.board);
            return;
        }

        bool isMaximizingPlayer = (node.type == Constants.MAX);
        double value = isMaximizingPlayer ? double.MinValue : double.MaxValue;

        foreach (Node child in node.childList)
        {
            EvaluateAndPropagateUtility(child);

            if (isMaximizingPlayer)
            {
                value = Mathf.Max((float)value, (float)child.utility);
                node.alfa = Mathf.Max((float)node.alfa, (float)value);
            }
            else
            {
                value = Mathf.Min((float)value, (float)child.utility);
                node.beta = Mathf.Min((float)node.beta, (float)value);
            }

            // Poda Alfa-Beta
            if (node.beta <= node.alfa)
                break;
        }

        node.utility = value;
    }

    int SelectBestMove(Node root)
    {
        double maxUtility = double.MinValue;
        int bestIndex = -1;

        for (int i = 0; i < root.childList.Count; i++)
        {
            if (root.childList[i].utility > maxUtility)
            {
                maxUtility = root.childList[i].utility;
                bestIndex = i;
            }
        }

        return bestIndex; // Asegura que bestIndex es -1 si no se encontró ningún movimiento mejor
    }
    double EvaluateBoard(Tile[] board)
    {
        int myPieces = boardManager.CountPieces(board, turn);
        int opponentPieces = boardManager.CountPieces(board, -turn);
        int score = myPieces - opponentPieces;

        int myMoves = boardManager.FindSelectableTiles(board, turn).Count;
        int opponentMoves = boardManager.FindSelectableTiles(board, -turn).Count;
        int mobilityScore = myMoves - opponentMoves;

        int myStablePieces = CountStablePieces(board, turn);
        int opponentStablePieces = CountStablePieces(board, -turn);
        int stabilityScore = myStablePieces - opponentStablePieces;

        int myCornerCount = CountCornerPieces(board, turn);
        int opponentCornerCount = CountCornerPieces(board, -turn);
        int cornersScore = (myCornerCount - opponentCornerCount) * 3;

        // Ajustar los pesos en función de la fase del juego
        int totalPieces = myPieces + opponentPieces;
        double phaseMultiplier = (totalPieces <= 20) ? 0.5 : // Early game
                                 (totalPieces <= 40) ? 1.0 : // Mid game
                                 1.5; // Late game

        // Calcula la utilidad final con pesos ajustables para cada componente
        return (score + 2 * mobilityScore + 5 * stabilityScore + cornersScore) * phaseMultiplier;
    }

    int CountStablePieces(Tile[] board, int player)
    {
        // Cuenta las piezas que no pueden ser volteadas en futuros movimientos
        int stable = 0;
        // Implementación simplificada, considera las piezas en las esquinas como estables
        int[] cornerIndices = { 0, 7, 56, 63 };
        foreach (int index in cornerIndices)
        {
            if (board[index].value == player)
            {
                stable++;
            }
        }
        return stable;
    }

    int CountCornerPieces(Tile[] board, int player)
    {
        int[] cornerIndices = { 0, 7, 56, 63 };
        int count = 0;
        foreach (int index in cornerIndices)
        {
            if (board[index].value == player)
            {
                count++;
            }
        }
        return count;
    }





}
