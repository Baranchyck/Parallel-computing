#include <iostream>
#include <vector>
#include <thread>
#include <cstdlib>   
#include <ctime>     
#include <cmath>
#include <windows.h>

constexpr double EPS = 1e-9;
constexpr int MAX_ITER = 10000;

double randomDouble(double min, double max) {
    return min + (double)rand() / RAND_MAX * (max - min);
}

void generateSLAR(int n, std::vector<std::vector<double>>& A, std::vector<double>& b) {
    srand(42); 

    A.assign(n, std::vector<double>(n));
    b.assign(n, 0.0);

    for (int i = 0; i < n; ++i) {
        double rowSum = 0.0;
        for (int j = 0; j < n; ++j) {
            if (i != j) {
                A[i][j] = randomDouble(-10.0, 10.0);
                rowSum += std::fabs(A[i][j]);
            }
        }
        A[i][i] = rowSum + std::fabs(randomDouble(-10.0, 10.0)) + 1.0;
        b[i] = randomDouble(-10.0, 10.0);
    }
}

double jacobiStep(int i, int n,
    const std::vector<std::vector<double>>& A,
    const std::vector<double>& b,
    const std::vector<double>& xOld)
{
    double weightedSum = 0.0;
    for (int j = 0; j < n; ++j) {
        if (j != i) weightedSum += A[i][j] * xOld[j];
    }
    return (b[i] - weightedSum) / A[i][i];
}

double crytery(const std::vector<double>& xOld, const std::vector<double>& xNew) {
    double diff = 0.0;
    for (size_t i = 0; i < xOld.size(); ++i) diff += std::fabs(xNew[i] - xOld[i]);
    return diff;
}

std::vector<double> jacobi(
    const std::vector<std::vector<double>>& A,
    const std::vector<double>& b,
    double eps, int maxIter)
{
    int n = A.size();
    std::vector<double> xOld(n, 0.0), xNew(n, 0.0);

    for (int iter = 0; iter < maxIter; ++iter) {
        for (int i = 0; i < n; ++i) {
            xNew[i] = jacobiStep(i, n, A, b, xOld);
        }

        double residualNorm = crytery(xOld, xNew);
        xOld.swap(xNew);
        if (residualNorm < eps) break;
    }
    return xOld;
}

std::vector<double> jacobiCooler(
    const std::vector<std::vector<double>>& A,
    const std::vector<double>& b,
    double eps, int maxIter, int numThreads)
{
    int n = A.size();
    std::vector<double> xOld(n, 0.0), xNew(n, 0.0);

    auto computeRange = [&](int start, int end) {
        for (int i = start; i < end; ++i) {
            xNew[i] = jacobiStep(i, n, A, b, xOld);
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

        double residualNorm = crytery(xOld, xNew);
        xOld.swap(xNew);
        if (residualNorm < eps) break;
    }
    return xOld;
}

void displayResult(int threads, double timeMs, double speedup) {
    std::cout << "  потоків=" << threads
        << " час=" << timeMs << " мс"
        << " прискорення=" << speedup << "\n";
}

int main() {
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);

    std::vector<int> sizes = { 100, 500, 1000, 2000, 4000, 8000, 16000, 32000 };
    std::vector<int> threadCounts = { 2, 4, 8, 16, 32, 64 };

    for (int n : sizes) {
        std::vector<std::vector<double>> A;
        std::vector<double> b;
        generateSLAR(n, A, b);

        clock_t t0 = clock();
        auto xSeq = jacobi(A, b, EPS, MAX_ITER);
        clock_t t1 = clock();
        double seqMs = 1000.0 * (t1 - t0) / CLOCKS_PER_SEC;

        std::cout << "n=" << n << " послідовно: " << seqMs << " мс\n";

        for (int k : threadCounts) {
            clock_t p0 = clock();
            auto xPar = jacobiCooler(A, b, EPS, MAX_ITER, k);
            clock_t p1 = clock();
            double parMs = 1000.0 * (p1 - p0) / CLOCKS_PER_SEC;

            displayResult(k, parMs, seqMs / parMs);
        }
    }
    return 0;
}