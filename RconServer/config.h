#pragma once
/* use #define GALAXY for GOG Galaxy .exe, leave blank to use Steam .exe */
//TODO: automatically detect game variant at runtime
//#define GALAXY

/* max. connections to the rcon server */
#define MAX_CONNECTIONS 100

/*
 * disables update rate limiter
 * CAUTION: can cause uncontrolled UDP output, only for testing purposes
 *			crashes clients to desktop	
 */
//#define EXPERIMENTAL_UPS