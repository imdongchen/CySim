#include "logFile.h"

void logFile(char *logInfo)
{
	FILE *file;
	file = fopen("e:\\log.txt", "a");
	if (file != NULL)
	{
		fprintf(file, logInfo);
	}
	fclose(file);
}

void logFile(long n)
{
	FILE *file;
	file = fopen("e:\\log.txt", "a");
	if (file != NULL)
	{
		char str[20];
		fprintf(file, itoa(n, str, 10));
	}
	fclose(file);
}