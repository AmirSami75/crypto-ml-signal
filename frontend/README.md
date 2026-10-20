# React operations dashboard

The active dashboard uses React 19, TypeScript, and Vite. The development server proxies
`/api` and `/health` to the .NET orchestrator, so browser code never calls the Python ML
service directly.

Run it through Docker Compose from `devops/`, or with Node 20.19+ / 22.12+.
