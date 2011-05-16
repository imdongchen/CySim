#pragma once

#include <stdio.h>
#include <stdlib.h>

void __declspec(dllexport) logFile(char *logInfo);
void __declspec(dllexport) logFile(long n);