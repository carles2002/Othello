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
    /*
    public int SelectTile(Tile[] board)
    {        
             
        //Generamos el nodo raíz del árbol (MAX)
        Node root = new Node(board);
        root.type = Constants.MAX;

        //Generamos primer nivel de nodos hijos
        List<int> selectableTiles = boardManager.FindSelectableTiles(board, turn);

        foreach (int s in selectableTiles)
        {
            //Creo un nuevo nodo hijo con el tablero padre
            Node n = new Node(root.board);
            //Lo añadimos a la lista de nodos hijo
            root.childList.Add(n);
            //Enlazo con su padre
            n.parent = root;
            //En nivel 1, los hijos son MIN
            n.type = Constants.MIN;
            //Aplico un movimiento, generando un nuevo tablero con ese movimiento
            boardManager.Move(n.board, s, turn);
            //si queremos imprimir el nodo generado (tablero hijo)
            //boardManager.PrintBoard(n.board);
        }

        //Selecciono un movimiento aleatorio. Esto habrá que modificarlo para elegir el mejor movimiento según MINIMAX
        int movimiento = Random.Range(0, selectableTiles.Count);

        return selectableTiles[movimiento];

    }
    */

    public int SelectTile(Tile[] board)
    {
        // Generamos el nodo raíz del árbol (MAX)
        Node root = new Node(board);
        root.type = Constants.MAX;

        // Generamos primer nivel de nodos hijos
        List<int> selectableTiles = boardManager.FindSelectableTiles(board, turn);

        foreach (int s in selectableTiles)
        {
            // Creo un nuevo nodo hijo con el tablero padre
            Node n = new Node(root.board);
            // Lo añadimos a la lista de nodos hijo
            root.childList.Add(n);
            // Enlazo con su padre
            n.parent = root;
            // En nivel 1, los hijos son MIN
            n.type = Constants.MIN;
            // Aplico un movimiento, generando un nuevo tablero con ese movimiento
            boardManager.Move(n.board, s, turn);
            // Generamos el siguiente nivel de nodos (profundidad 2 a 4)
            GenerateMinimaxTree(n, 1); // Aquí pasamos el nivel actual como 1
        }

        // Implementar la evaluación de utilidad y propagar valores
        EvaluateAndPropagateUtility(root);

        // Elegir el mejor movimiento basado en la utilidad
        int bestMove = SelectBestMove(root);
        return selectableTiles[bestMove];
    }

    void GenerateMinimaxTree(Node node, int currentDepth)
    {
        if (currentDepth >= 4) return; // Detener a profundidad 4

        // Tipo alterno para el siguiente nivel
        int nextType = (node.type == Constants.MAX ? Constants.MIN : Constants.MAX);
        List<int> nextMoves = boardManager.FindSelectableTiles(node.board, turn);

        foreach (int move in nextMoves)
        {
            Node childNode = new Node(node.board);
            boardManager.Move(childNode.board, move, turn);
            childNode.type = nextType;
            node.childList.Add(childNode);

            // Recursión para los siguientes niveles
            GenerateMinimaxTree(childNode, currentDepth + 1);
        }
    }

    void EvaluateAndPropagateUtility(Node node)
    {
        // Base case: si es un nodo hoja, calcula su utilidad directamente
        if (node.childList.Count == 0)
        {
            node.utility = EvaluateBoard(node.board);
            return;
        }

        // Recursivamente evalúa y propaga desde los hijos primero
        foreach (Node child in node.childList)
        {
            EvaluateAndPropagateUtility(child);
        }

        // Propaga la utilidad hacia arriba según si es nodo MIN o MAX
        node.utility = (node.type == Constants.MAX ? node.childList.Max(n => n.utility) : node.childList.Min(n => n.utility));
    }

    int SelectBestMove(Node root)
    {
        // Suponiendo que el nodo raíz es un nodo MAX
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

        return bestIndex;
    }

    double EvaluateBoard(Tile[] board)
    {
        // Puntuaciones básicas
        int myPieces = boardManager.CountPieces(board, turn);
        int opponentPieces = boardManager.CountPieces(board, -turn);
        int score = myPieces - opponentPieces;

        // Puntuación por movilidad
        int myMoves = boardManager.FindSelectableTiles(board, turn).Count;
        int opponentMoves = boardManager.FindSelectableTiles(board, -turn).Count;
        int mobilityScore = myMoves - opponentMoves;

        // Puntuación por estabilidad
        int myStablePieces = CountStablePieces(board, turn);
        int opponentStablePieces = CountStablePieces(board, -turn);
        int stabilityScore = myStablePieces - opponentStablePieces;

        // Puntuación por posición estratégica (por ejemplo, esquinas)
        int myCornerCount = CountCornerPieces(board, turn);
        int opponentCornerCount = CountCornerPieces(board, -turn);
        int cornersScore = (myCornerCount - opponentCornerCount) * 3; // Las esquinas pueden tener un peso mayor

        // Calcula la utilidad final con pesos ajustables para cada componente
        return score + 2 * mobilityScore + 5 * stabilityScore + cornersScore;
    }

    int CountStablePieces(Tile[] board, int player)
    {
        // Implementa una lógica para contar las piezas estables
        return 0; // Placeholder
    }

    int CountCornerPieces(Tile[] board, int player)
    {
        int[] cornerIndices = { 0, 7, 56, 63 }; // Índices de las esquinas en un tablero 8x8

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
