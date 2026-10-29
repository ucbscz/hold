const { env } = require("process");

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:5190`
  : env.ASPNETCORE_URLS
    ? env.ASPNETCORE_URLS.split(";")[0]
    : "http://localhost:5190";

const PROXY_CONFIG = [
  {
    context: ["/api"],
    target,
    secure: false,
  },
];

module.exports = PROXY_CONFIG;
