function fn() {
    var env = karate.env || 'dev';
    var config = {
        baseUrl: 'https://localhost:7127'
    };

    if (env === 'azure') {
        config.baseUrl = 'https://mnemotoad-learning-dng0c0chbcfufwg8.westus3-01.azurewebsites.net/';
    }

    if (env === 'dev') {
        karate.configure('ssl', { trustAll: true });
    }

    return config;
}
