CLOCKWORKRT.collisions.register({
    shape1: "plasma",
    shape2: "destructible",
    detector: function (p, b, data) {
        if ((p.x - b.x) *(p.x - b.x) + (p.y - b.y)*(p.y - b.y) + (p.z - b.z)*(p.z - b.z) < 3600) {
            return true;
        } else {
            return false;
        }
    }
});
