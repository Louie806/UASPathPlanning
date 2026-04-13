using System.Collections.Generic;

public class PriorityQueue<Node> {
    private struct Entry {
        public Node Node;
        public float Priority;

        public Entry(Node node, float priority) {
            Node = node;
            Priority = priority;
        }
    }

    private readonly List<Entry> heap = new();

    public int Count => heap.Count;

    public void Enqueue(Node node, float priority) {
        heap.Add(new Entry(node, priority));
        HeapifyUp(heap.Count - 1);
    }

    public Node Dequeue() {
        if (heap.Count == 0) throw new System.InvalidOperationException("Priority queue is empty.");

        Node rootNode = heap[0].Node;
        int lastIndex = heap.Count - 1;

        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        if (heap.Count > 0) HeapifyDown(0);

        return rootNode;
    }

    private void HeapifyUp(int index) {
        while (index > 0) {
            int parentIndex = (index - 1) / 2;

            if (heap[index].Priority >= heap[parentIndex].Priority)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index) {
        int lastIndex = heap.Count - 1;

        while (true) {
            int leftChild = index * 2 + 1;
            int rightChild = index * 2 + 2;
            int smallest = index;

            if (leftChild <= lastIndex && heap[leftChild].Priority < heap[smallest].Priority)
                smallest = leftChild;

            if (rightChild <= lastIndex && heap[rightChild].Priority < heap[smallest].Priority)
                smallest = rightChild;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b) {
        Entry temp = heap[a];
        heap[a] = heap[b];
        heap[b] = temp;
    }
}