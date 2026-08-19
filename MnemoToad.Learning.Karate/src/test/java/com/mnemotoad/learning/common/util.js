function(label) {
    var suffix = label ? ('_' + label) : '';
    return 'ZZ_Karate' + suffix + '_' + java.util.UUID.randomUUID();
}