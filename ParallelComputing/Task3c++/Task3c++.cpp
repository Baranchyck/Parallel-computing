#include <iostream>
#include <vector>
#include <thread>
#include <random>
#include <cmath>
#include <chrono>
#include <windows.h>

void generateSystem(int n, std::vector<std::vector<double>>& A, std::vector<double>& b) {
    std::mt19937 gen(42);
    std::uniform_real_distribution<double> dist(-10.0, 10.0);

    A.assign(n, std::vector<double>(n));
    b.assign(n, 0.0);

    for (int i = 0; i < n; ++i) {
        double rowSum = 0.0;
        for (int j = 0; j < n; ++j) {
            if (i != j) {
                A[i][j] = dist(gen);
                rowSum += std::fabs(A[i][j]);
            }
        }
        A[i][i] = rowSum + std::fabs(dist(gen)) + 1.0;
        b[i] = dist(gen);
    }
}

std::vector<double> jacobiSequential(
    const std::vector<std::vector<double>>& A,
    const std::vector<double>& b,
    double eps, int maxIter)
{
    int n = A.size();
    std::vector<double> xOld(n, 0.0), xNew(n, 0.0);

    for (int iter = 0; iter < maxIter; ++iter) {
        for (int i = 0; i < n; ++i) {
            double sum = 0.0;
            for (int j = 0; j < n; ++j) {
                if (j != i) sum += A[i][j] * xOld[j];
            }
            xNew[i] = (b[i] - sum) / A[i][i];
        }

        double diff = 0.0;
        for (int i = 0; i < n; ++i) diff += std::fabs(xNew[i] - xOld[i]);
        xOld.swap(xNew);
        if (diff < eps) break;
    }
    return xOld;
}

std::vector<double> jacobiParallel(
    const std::vector<std::vector<double>>& A,
    const std::vector<double>& b,
    double eps, int maxIter, int numThreads)
{
    int n = A.size();
    std::vector<double> xOld(n, 0.0), xNew(n, 0.0);

    auto computeRange = [&](int start, int end) {
        for (int i = start; i < end; ++i) {
            double sum = 0.0;
            for (int j = 0; j < n; ++j) {
                if (j != i) sum += A[i][j] * xOld[j];
            }
            xNew[i] = (b[i] - sum) / A[i][i];
        }
        };

    int chunk = n / numThreads;

    for (int iter = 0; iter < maxIter; ++iter) {
        std::vector<std::thread> threads;
        for (int t = 0; t < numThreads; ++t) {
            int start = t * chunk;
            int end = (t == numThreads - 1) ? n : start + chunk;
            threads.emplace_back(computeRange, start, end);
        }
        for (auto& t : threads) t.join();

        double diff = 0.0;
        for (int i = 0; i < n; ++i) diff += std::fabs(xNew[i] - xOld[i]);
        xOld.swap(xNew);
        if (diff < eps) break;
    }
    return xOld;
}

int main() {
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);

    std::vector<int> sizes = { 100, 500, 1000, 2000, 4000, 8000, 16000, 32000 };
    std::vector<int> threadCounts = { 2, 4, 8, 16, 32, 64 };

    for (int n : sizes) {
        std::vector<std::vector<double>> A;
        std::vector<double> b;
        generateSystem(n, A, b);

        auto t0 = std::chrono::high_resolution_clock::now();
        auto xSeq = jacobiSequential(A, b, 1e-9, 10000);
        auto t1 = std::chrono::high_resolution_clock::now();
        double seqMs = std::chrono::duration<double, std::milli>(t1 - t0).count();

        std::cout << "n=" << n << " послідовно: " << seqMs << " мс\n";

        for (int k : threadCounts) {
            auto p0 = std::chrono::high_resolution_clock::now();
            auto xPar = jacobiParallel(A, b, 1e-9, 10000, k);
            auto p1 = std::chrono::high_resolution_clock::now();
            double parMs = std::chrono::duration<double, std::milli>(p1 - p0).count();

            std::cout << "  потоків=" << k
                << " час=" << parMs << " мс"
                << " прискорення=" << (seqMs / parMs) << "\n";
        }
    }
    return 0;
}