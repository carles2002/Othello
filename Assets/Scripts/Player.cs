using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node
{
    // Representa el estado del tablero en este nodo
    public Tile[] board = new Tile[Constants.NumTiles];
    // Referencia al nodo padre
    public Node parent;
    // Lista de nodos hijos, que representan posibles movimientos futuros
    public List<Node> childList = new List<Node>();
    // Indica si el nodo es de tipo MAX o MIN
    public int type; // Constants.MIN o Constants.MAX
    // Valor de utilidad calculado para este nodo
    public double utility;
    // Valor alfa para la poda alfa-beta
    public double alfa;
    // Valor beta para la poda alfa-beta
    public double beta;

    // Constructor para crear un nodo a partir del estado del tablero dado
    public Node(Tile[] tiles)
    {
        // Copiamos el estado del tablero al nuevo nodo
        for (int i = 0; i < tiles.Length; i++)
        {
            this.board[i] = new Tile();
            this.board[i].value = tiles[i].value;
        }
    }
}

public class Player : MonoBehaviour
{
    // Indica el turno del jugador
    public int turn;
    // Referencia al gestor del tablero
    private BoardManager boardManager;

   
    void Start()
    {
        boardManager = GameObject.FindGameObjectWithTag("BoardManager").GetComponent<BoardManager>();
    }

    /*
     * Método SelectTile
     * Entrada: Estado actual del tablero
     * Salida: Posición donde mover
     */
    public int SelectTile(Tile[] board)
    {
        // Creamos el nodo raíz del árbol Minimax
        Node root = new Node(board);
        root.type = Constants.MAX; // El nodo raíz es un nodo MAX

        // Generamos el árbol Minimax con poda alfa-beta desde el nodo raíz
        GenerateMinimaxTree(root, 1, double.MinValue, double.MaxValue);

        // Obtener la lista de movimientos seleccionables 
        List<int> selectableTiles = boardManager.FindSelectableTiles(board, turn);

        // Si no hay movimientos disponibles, retornamos un valor indicando error
        if (selectableTiles.Count == 0)
        {
            Debug.LogError("No hay movimientos disponibles.");
            return -1; 
        }

        // Seleccionamos el mejor movimiento basado en la utilidad calculada
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

    /*
     * Método GenerateMinimaxTree
     * Construye el árbol de decisiones Minimax con poda alfa-beta
     */
    void GenerateMinimaxTree(Node node, int currentDepth, double alpha, double beta)
    {
        if (currentDepth >= 4) return; // Detener a profundidad 4

        // Determina si es un nodo MAX o MIN
        bool isMaximizingPlayer = (node.type == Constants.MAX);
        // Obtiene los posibles movimientos desde el estado actual del tablero
        List<int> nextMoves = boardManager.FindSelectableTiles(node.board, turn);

        // Itera sobre cada posible movimiento
        foreach (int move in nextMoves)
        {
            // Crea un nuevo nodo hijo aplicando el movimiento
            Node childNode = new Node(node.board);
            boardManager.Move(childNode.board, move, turn);
            // Alterna el tipo del nodo hijo
            childNode.type = isMaximizingPlayer ? Constants.MIN : Constants.MAX;
            node.childList.Add(childNode);

            // Genera recursivamente el siguiente nivel del árbol
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

    /*
     * Método EvaluateAndPropagateUtility
     * Evalúa y propaga los valores de utilidad a través del árbol Minimax
     */
    void EvaluateAndPropagateUtility(Node node)
    {
        // Si el nodo es el ultimo del arbol, evaluamos directamente su utilidad
        if (node.childList.Count == 0)
        {
            node.utility = EvaluateBoard(node.board);
            return;
        }

        // Determina si es un nodo MAX o MIN
        bool isMaximizingPlayer = (node.type == Constants.MAX);
        double value = isMaximizingPlayer ? double.MinValue : double.MaxValue;

        // Evalúa recursivamente los nodos hijos
        foreach (Node child in node.childList)
        {
            // Llamada recursiva para evaluar y propagar la utilidad del nodo hijo
            EvaluateAndPropagateUtility(child);

            // Si el nodo actual es un nodo MAX, buscamos maximizar el valor de utilidad
            if (isMaximizingPlayer)
            {
                // Actualizamos el valor máximo encontrado entre los nodos hijos
                value = Mathf.Max((float)value, (float)child.utility);
                // Actualizamos el valor alfa del nodo actual
                node.alfa = Mathf.Max((float)node.alfa, (float)value);
            }
            else // Si el nodo actual es un nodo MIN, buscamos minimizar el valor de utilidad
            {
                // Actualizamos el valor mínimo encontrado entre los nodos hijos
                value = Mathf.Min((float)value, (float)child.utility);
                // Actualizamos el valor beta del nodo actual
                node.beta = Mathf.Min((float)node.beta, (float)value);
            }

            // Poda Alfa-Beta: si el valor beta del nodo es menor o igual al valor alfa
            // del nodo padre, no necesitamos explorar más nodos hijos
            if (node.beta <= node.alfa)
                break;
        }

        // Asignamos el valor de utilidad al nodo actual
        node.utility = value;

    }

    /*
     * Método SelectBestMove
     * Selecciona el mejor movimiento basado en la utilidad calculada
     */
    int SelectBestMove(Node root)
    {
        double maxUtility = double.MinValue;
        int bestIndex = -1;

        // Itera sobre los nodos hijos del nodo raíz para encontrar la mayor utilidad
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

    /*
     * Método EvaluateBoard
     * Calcula la utilidad del tablero basado en varios factores estratégicos
     */
    double EvaluateBoard(Tile[] board)
    {
        // Contamos cuántas piezas tiene el jugador actual
        int myPieces = boardManager.CountPieces(board, turn);
        // Contamos cuántas piezas tiene el oponente
        int opponentPieces = boardManager.CountPieces(board, -turn);
        // Calculamos la diferencia de piezas entre el jugador actual y el oponente
        int score = myPieces - opponentPieces;

        // Contamos el número de movimientos posibles para el jugador actual
        int myMoves = boardManager.FindSelectableTiles(board, turn).Count;
        // Contamos el número de movimientos posibles para el oponente
        int opponentMoves = boardManager.FindSelectableTiles(board, -turn).Count;
        // Calculamos la diferencia de movilidad entre el jugador actual y el oponente
        int mobilityScore = myMoves - opponentMoves;

        // Contamos el número de piezas estables del jugador actual
        int myStablePieces = CountStablePieces(board, turn);
        // Contamos el número de piezas estables del oponente
        int opponentStablePieces = CountStablePieces(board, -turn);
        // Calculamos la diferencia de estabilidad entre el jugador actual y el oponente
        int stabilityScore = myStablePieces - opponentStablePieces;

        // Contamos el número de esquinas ocupadas por el jugador actual
        int myCornerCount = CountCornerPieces(board, turn);
        // Contamos el número de esquinas ocupadas por el oponente
        int opponentCornerCount = CountCornerPieces(board, -turn);
        // Calculamos la diferencia de control de esquinas, asignando un peso mayor a las esquinas
        int cornersScore = (myCornerCount - opponentCornerCount) * 3;

        // Ajustar los pesos en función de la fase del juego
        // Contamos el número total de piezas en el tablero
        int totalPieces = myPieces + opponentPieces;
        // Determinamos el multiplicador de fase basado en el número total de piezas
        double phaseMultiplier = (totalPieces <= 20) ? 0.5 : // Principio del juego
                                 (totalPieces <= 40) ? 1.0 : // A mitad del juego
                                 1.5; // Casi al final

        // Calcula la utilidad final sumando las diferentes puntuaciones ponderadas por el multiplicador de fase
        return (score + 2 * mobilityScore + 5 * stabilityScore + cornersScore) * phaseMultiplier;
    }


    /*
     * Método CountStablePieces
     * Cuenta las piezas que no pueden ser volteadas en futuros movimientos
     */
    int CountStablePieces(Tile[] board, int player)
    {
        int stable = 0;
        // Considera las piezas en las esquinas como estables
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

    /*
     * Método CountCornerPieces
     * Cuenta el número de piezas que el jugador tiene en las esquinas del tablero
     */
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
