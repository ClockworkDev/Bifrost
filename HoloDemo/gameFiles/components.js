CLOCKWORKRT.components.register([
    {
        name: "background",
        sprite: "background"
    },
    {
        name: "bomb",
        sprite: "bomb",
        events: [
            {
                name: "#setup", code: function (event) {
                    this.var.t = 0;
                }
            },
            {
                name: "#loop", code: function (event) {
                    this.var.t++;
                    if (this.var.t == 180) {
                        this.var.$state = "warning";
                    }
                    if (this.var.t == 300) {
                        this.var.$state = "danger";
                    }
                    if (this.var.t == 500) {
                        this.engine.addObjectLive("badExplosion", "badExplosion", this.var.$x, this.var.$y, this.var.$z);
                        this.engine.deleteObjectLive(this);
                    }
                }
            },
            {
                name: "#collide", code: function (event) {
                    if (event.shape2tag == "plasma") {
                        this.engine.addObjectLive("goodExplosion", "goodExplosion", this.var.$x, this.var.$y, this.var.$z);
                        this.engine.deleteObjectLive(this);
                    }
                }
            }
        ],
        collision: {
            destructible: [
                { "#tag": "bomb", x: 0, y: 0, z: 0 },
            ]
        },
    },
    {
        name: "projectile",
        sprite: "plasma",
        events: [
            {
                name: "#setup", code: function (data) {
                    this.var.t = 0;
                }
            },
            {
                name: "#loop", code: function (data) {
                    this.var.$x += this.var.vx;
                    this.var.$y += this.var.vy;
                    this.var.$z += this.var.vz;
                    if (this.var.t++ > 300) {
                        this.engine.deleteObjectLive(this);
                    }
                }
            },
            {
                name: "#collide", code: function (event) {
                    if (event.shape2tag == "bomb") {
                        this.engine.deleteObjectLive(this);
                    }
                }
            }
        ],
        collision: {
            plasma: [
                { "#tag": "plasma", x: 0, y: 0, z: 0 },
            ]
        },
    },
    {
        name: "player",
        sprite: "player",
        events: [
            {
                name: "#setup", code: function (data) {
                    this.var.$angle = 0;
                }
            },
            {
                name: "updatePlayerPosition", code: function (data) {
                    this.var.$x = data.x;
                    this.var.$y = data.y;
                    this.engine.do.updateCameraPosition(data);
                }
            },
            {
                name: "updatePlayerGaze", code: function (data) {
                    this.var.$angle = Math.atan2(data.y, data.x);
                }
            },
            {
                name: "shootPlayerProjectile", code: function (data) {
                    var distance = 40;
                    var projectile = this.engine.addObjectLive("someProjectile", "projectile", data.head.x + data.gaze.x * distance, data.head.y + data.gaze.y * distance, data.head.z + data.gaze.z * distance);
                    var speed = 10;
                    projectile.var.vx = data.gaze.x * speed;
                    projectile.var.vy = data.gaze.y * speed;
                    projectile.var.vz = data.gaze.z * speed;
                }
            }
        ]
    },
    {
        name: "objectSpawner",
        events: [
            {
                name: "#setup", code: function (data) {
                    this.var.dx = 0;
                    this.var.dy = 0;
                }
            },
            {
                name: "updateCameraPosition", code: function (data) {
                    this.engine.var.$cameraX = data.x - 1266 / 2;
                    this.engine.var.$cameraY = data.y - 768 / 2;
                }
            },
            {
                name: "#collide", code: function (event) {
                    if (event.shape2tag == "click") {
                        this.engine.addObjectLive("someBomb", "bomb", this.engine.var.$cameraX + event.data.x * 1266, this.engine.var.$cameraY + event.data.y * 768, 0);
                    }
                }
            }
        ],
        collision: {
            box: [
                { "#tag": "boundingBox", x: 0, y: 0, w: 1266, h: 768 },
            ]
        }
    },
    {
        name: "incomingDataHandler",
        events: [
            {
                name: "#setup", code: function (event) {
                    this.engine.getAnimationEngine().sendCommand("registerEngineMessageHandler", this.do.incomingData);
                }
            },
            {
                name: "incomingData", code: function (event) {
                    switch (event.data[0]) {
                        case "move":
                            this.engine.do.updatePlayerPosition({ x: event.data[1].X, y: event.data[1].Z, z: event.data[1].Y });
                            this.engine.do.updatePlayerGaze({ x: event.data[2].X, y: event.data[2].Z, z: event.data[2].Y });
                            break;
                        case "tap":
                            this.engine.do.shootPlayerProjectile({ head: { x: event.data[1].X, y: event.data[1].Z, z: event.data[1].Y }, gaze: { x: event.data[2].X, y: event.data[2].Z, z: event.data[2].Y } });
                            break;
                        case "surfaceAdded":
                            this.engine.do.surfaceAdded({ id: event.data[1], vertexes: event.data[2], indexes: event.data[3] });
                            break;
                        case "surfaceModified":
                            this.engine.do.surfaceModified({ id: event.data[1], vertexes: event.data[2], indexes: event.data[3] });
                            break;
                        case "surfaceRemoved":
                            this.engine.do.surfaceRemoved({ id: event.data[1] });
                            break;
                    }
                }
            }
        ]
    },
    {
        name: "meshRenderer",
        sprite: "meshRenderer",
        events: [
            {
                name: "#setup", code: function (event) {
                    this.var.$vertexes = {};
                    this.var.$images = {};
                }
            },
            {
                name: "surfaceAdded", code: function (event) {
                    this.var.$vertexes[event.id] = event.indexes.map(function (i) { return event.vertexes[i] });
                    this.do.generateSurface(event.id);
                }
            },
            {
                name: "surfaceModified", code: function (event) {
                    this.var.$vertexes[event.id] = event.indexes.map(function (i) { return event.vertexes[i] });
                    this.do.generateSurface(event.id);
                }
            },
            {
                name: "surfaceRemoved", code: function (event) {
                    this.var.$vertexes[event.id] = undefined;
                    this.var.$images[event.id] = undefined;
                }
            },
            {
                name: "generateSurface", code: function (id) {
                    var vertexes = this.vars.$vertexes[id];
                    var canvas = document.createElement('canvas');
                    var minX = vertexes.map(function (x) { return x.X }).reduce(function (a, b) { return a > b ? b : a; });
                    var maxX = vertexes.map(function (x) { return x.X }).reduce(function (a, b) { return a < b ? b : a; });
                    var minZ = vertexes.map(function (x) { return x.Z }).reduce(function (a, b) { return a > b ? b : a; });
                    var maxZ = vertexes.map(function (x) { return x.Z }).reduce(function (a, b) { return a < b ? b : a; });
                    canvas.width = maxX - minX;
                    canvas.height = maxZ - minZ;
                    var context = canvas.getContext("2d");
                    context.lineWidth = 1;
                    for (var i = 0; i < vertexes.length; i += 3) {
                        var a1 = vertexes[i];
                        var a2 = vertexes[i + 1];
                        var a3 = vertexes[i + 2];
                        var depth = (a1.Y + a2.Y + a3.Y) / 2;
                        var r = (depth - (-400)) / (800) * 255;
                        var b = 255 - r;
                        context.beginPath();
                        context.moveTo(a1.X - minX, a1.Z - minZ);
                        context.lineTo(a2.X - minX, a2.Z - minZ);
                        context.lineTo(a3.X - minX, a3.Z - minZ);
                        context.lineTo(a1.X - minX, a1.Z - minZ);
                        context.strokeStyle = 'rgba(' + r + ',' + b + ',255,0.8)';
                        context.stroke();
                        context.fillStyle = 'rgba(' + r + ',' + b + ',255,0.3)';
                        context.fill();
                    }
                    this.var.$images[id] = { canvas: canvas, x: minX, y: minZ };
                }
            }
        ]
    },
    {
        name: "explosion",
        sprite: "explosion",
        events: [
            {
                name: "#setup", code: function (event) {
                    this.var.t = 0;
                }
            },
            {
                name: "#loop", code: function (event) {
                    if (this.var.t++ > 60) {
                        this.engine.deleteObjectLive(this);
                    }
                }
            }
        ]
    },
    {
        name: "goodExplosion",
        inherits: "explosion",
        vars: [
            { name: "$state", value: "good" }
        ]
    },
    {
        name: "badExplosion",
        inherits: "explosion",
        vars: [
            { name: "$state", value: "bad" }
        ]
    }
]);