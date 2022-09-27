var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
class CanvasDrawer {
    constructor(ctx) {
        this._argTerminator = ";";
        this._cmdTerminator = "|";
        this._ctx = ctx;
    }
    getNextArg() {
        var endIndex = this._commandList.indexOf(this._argTerminator, this._currentCommandIndex);
        if (endIndex < 0) {
            console.log("Missing argument at index " + this._currentCommandIndex);
            return null;
        }
        var arg = this._commandList.slice(this._currentCommandIndex, endIndex);
        this._currentCommandIndex = endIndex + 1;
        return arg;
    }
    getFloatArg() {
        var arg = this.getNextArg();
        var floatArg = Number(arg);
        if (isNaN(floatArg)) {
            console.log("Expected a number argument, got '" + arg + "'");
        }
        return floatArg;
    }
    getIntArg() {
        var arg = this.getNextArg();
        var intArg = parseInt(arg);
        if (isNaN(intArg)) {
            console.log("Expected a number argument, got '" + arg + "'");
        }
        return intArg;
    }
    getStringArg() {
        return this.getNextArg();
    }
    save() {
        this._ctx.save();
    }
    restore() {
        this._ctx.restore();
    }
    fillStyle() {
        var color = this.getStringArg();
        this._ctx.fillStyle = color;
    }
    lineStyle() {
        var width = this.getFloatArg();
        var color = this.getStringArg();
        var join = this.getStringArg();
        var cap = this.getStringArg();
        var miterLimit = this.getFloatArg();
        this._ctx.strokeStyle = color;
        this._ctx.lineWidth = width;
        this._ctx.lineJoin = join;
        this._ctx.lineCap = cap;
        this._ctx.miterLimit = miterLimit;
    }
    drawRect() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        this._ctx.strokeRect(x1, y1, x2 - x1, y2 - y1);
    }
    fillRect() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        this._ctx.fillRect(x1, y1, x2 - x1, y2 - y1);
    }
    drawEllipse() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        this._ctx.beginPath();
        this._ctx.ellipse((x1 + x2) / 2, (y1 + y2) / 2, (x2 - x1) / 2, (y2 - y1) / 2, 0, 0, 2 * Math.PI);
        this._ctx.stroke();
    }
    fillEllipse() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        this._ctx.beginPath();
        this._ctx.ellipse((x1 + x2) / 2, (y1 + y2) / 2, (x2 - x1) / 2, (y2 - y1) / 2, 0, 0, 2 * Math.PI);
        this._ctx.fill();
    }
    arc() {
        var x = this.getFloatArg();
        var y = this.getFloatArg();
        var r = this.getFloatArg();
        var startAngle = this.getFloatArg();
        var endAngle = this.getFloatArg();
        this._ctx.beginPath();
        this._ctx.arc(x, y, r, startAngle, endAngle);
        this._ctx.stroke();
    }
    beginPath() {
        this._ctx.beginPath();
    }
    closePath() {
        this._ctx.closePath();
    }
    fillPath() {
        var fillRule = this.getIntArg();
        if (fillRule != 0) {
            this._ctx.fill("nonzero");
        }
        else {
            this._ctx.fill("evenodd");
        }
    }
    drawPath() {
        this._ctx.stroke();
    }
    clipPath() {
        var fillRule = this.getIntArg();
        if (fillRule != 0) {
            this._ctx.clip("nonzero");
        }
        else {
            this._ctx.clip("evenodd");
        }
    }
    moveTo() {
        var x = this.getFloatArg();
        var y = this.getFloatArg();
        this._ctx.moveTo(x, y);
    }
    lineTo() {
        var x = this.getFloatArg();
        var y = this.getFloatArg();
        this._ctx.lineTo(x, y);
    }
    bezierTo() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        var x3 = this.getFloatArg();
        var y3 = this.getFloatArg();
        this._ctx.bezierCurveTo(x1, y1, x2, y2, x3, y3);
    }
    transform() {
        var m11 = this.getFloatArg();
        var m12 = this.getFloatArg();
        var m21 = this.getFloatArg();
        var m22 = this.getFloatArg();
        var mdx = this.getFloatArg();
        var mdy = this.getFloatArg();
        this._ctx.transform(m11, m12, m21, m22, mdx, mdy);
    }
    drawSingleCommand() {
        var whichCommand = this._commandList.charAt(this._currentCommandIndex);
        this._currentCommandIndex += 1;
        switch (whichCommand) {
            case 'S':
                this.fillStyle();
                break;
            case 's':
                this.lineStyle();
                break;
            case 'r':
                this.drawRect();
                break;
            case 'R':
                this.fillRect();
                break;
            case 'e':
                this.drawEllipse();
                break;
            case 'E':
                this.fillEllipse();
                break;
            case 'a':
                this.arc();
                break;
            case 'p':
                this.beginPath();
                break;
            case 'c':
                this.closePath();
                break;
            case 'm':
                this.moveTo();
                break;
            case 'l':
                this.lineTo();
                break;
            case 'b':
                this.bezierTo();
                break;
            case 'f':
                this.fillPath();
                break;
            case 'd':
                this.drawPath();
                break;
            case 'C':
                this.clipPath();
                break;
            case 'x':
                this.transform();
                break;
            case 'z':
                this.save();
                break;
            case 'Z':
                this.restore();
                break;
        }
        // Move to beginning of next command.
        this._currentCommandIndex = this._commandList.indexOf(this._cmdTerminator, this._currentCommandIndex);
        if (this._currentCommandIndex > 0) {
            this._currentCommandIndex += 1;
        }
    }
    drawCommands(commands) {
        this._commandList = commands;
        this._currentCommandIndex = 0;
        this._ctx.save();
        while (true) {
            if (this._currentCommandIndex < 0 || this._currentCommandIndex >= this._commandList.length)
                break;
            this.drawSingleCommand();
        }
        this._ctx.restore();
    }
}
class ParseOnlyCanvasDrawer {
    constructor() {
        this._argTerminator = ";";
        this._cmdTerminator = "|";
    }
    getNextArg() {
        var endIndex = this._commandList.indexOf(this._argTerminator, this._currentCommandIndex);
        if (endIndex < 0) {
            console.log("Missing argument at index " + this._currentCommandIndex);
            return null;
        }
        var arg = this._commandList.slice(this._currentCommandIndex, endIndex);
        this._currentCommandIndex = endIndex + 1;
        return arg;
    }
    getFloatArg() {
        var arg = this.getNextArg();
        var floatArg = Number(arg);
        if (isNaN(floatArg)) {
            console.log("Expected a number argument, got '" + arg + "'");
        }
        return floatArg;
    }
    getIntArg() {
        var arg = this.getNextArg();
        var intArg = parseInt(arg);
        if (isNaN(intArg)) {
            console.log("Expected a number argument, got '" + arg + "'");
        }
        return intArg;
    }
    getStringArg() {
        return this.getNextArg();
    }
    save() {
        //this._ctx.save();
    }
    restore() {
        //this._ctx.restore();
    }
    fillStyle() {
        var color = this.getStringArg();
        //this._ctx.fillStyle = color;
    }
    lineStyle() {
        var width = this.getFloatArg();
        var color = this.getStringArg();
        var join = this.getStringArg();
        var cap = this.getStringArg();
        var miterLimit = this.getFloatArg();
        //this._ctx.strokeStyle = color;
        //this._ctx.lineWidth = width;
        //this._ctx.lineJoin = <CanvasLineJoin>join;
        //this._ctx.lineCap = <CanvasLineCap>cap;
        //this._ctx.miterLimit = miterLimit;
    }
    drawRect() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        //this._ctx.strokeRect(x1, y1, x2 - x1, y2 - y1);
    }
    fillRect() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        //this._ctx.fillRect(x1, y1, x2 - x1, y2 - y1);
    }
    drawEllipse() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        //this._ctx.beginPath();
        //this._ctx.ellipse((x1 + x2) / 2, (y1 + y2) / 2, (x2 - x1) / 2, (y2 - y1) / 2, 0, 0, 2 * Math.PI);
        //this._ctx.stroke();
    }
    fillEllipse() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        //this._ctx.beginPath();
        //this._ctx.ellipse((x1 + x2) / 2, (y1 + y2) / 2, (x2 - x1) / 2, (y2 - y1) / 2, 0, 0, 2 * Math.PI);
        //this._ctx.fill();
    }
    arc() {
        var x = this.getFloatArg();
        var y = this.getFloatArg();
        var r = this.getFloatArg();
        var startAngle = this.getFloatArg();
        var endAngle = this.getFloatArg();
        //this._ctx.beginPath();
        //this._ctx.arc(x, y, r, startAngle, endAngle);
        //this._ctx.stroke();
    }
    beginPath() {
        //this._ctx.beginPath();
    }
    closePath() {
        //this._ctx.closePath();
    }
    fillPath() {
        var fillRule = this.getIntArg();
        if (fillRule != 0) {
            //this._ctx.fill("nonzero");
        }
        else {
            //this._ctx.fill("evenodd");
        }
    }
    drawPath() {
        //this._ctx.stroke();
    }
    clipPath() {
        var fillRule = this.getIntArg();
        if (fillRule != 0) {
            //this._ctx.clip("nonzero");
        }
        else {
            //this._ctx.clip("evenodd");
        }
    }
    moveTo() {
        var x = this.getFloatArg();
        var y = this.getFloatArg();
        //this._ctx.moveTo(x, y);
    }
    lineTo() {
        var x = this.getFloatArg();
        var y = this.getFloatArg();
        //this._ctx.lineTo(x, y);
    }
    bezierTo() {
        var x1 = this.getFloatArg();
        var y1 = this.getFloatArg();
        var x2 = this.getFloatArg();
        var y2 = this.getFloatArg();
        var x3 = this.getFloatArg();
        var y3 = this.getFloatArg();
        //this._ctx.bezierCurveTo(x1, y1, x2, y2, x3, y3);
    }
    transform() {
        var m11 = this.getFloatArg();
        var m12 = this.getFloatArg();
        var m21 = this.getFloatArg();
        var m22 = this.getFloatArg();
        var mdx = this.getFloatArg();
        var mdy = this.getFloatArg();
        //this._ctx.transform(m11, m12, m21, m22, mdx, mdy);
    }
    drawSingleCommand() {
        var whichCommand = this._commandList.charAt(this._currentCommandIndex);
        this._currentCommandIndex += 1;
        switch (whichCommand) {
            case 'S':
                this.fillStyle();
                break;
            case 's':
                this.lineStyle();
                break;
            case 'r':
                this.drawRect();
                break;
            case 'R':
                this.fillRect();
                break;
            case 'e':
                this.drawEllipse();
                break;
            case 'E':
                this.fillEllipse();
                break;
            case 'a':
                this.arc();
                break;
            case 'p':
                this.beginPath();
                break;
            case 'c':
                this.closePath();
                break;
            case 'm':
                this.moveTo();
                break;
            case 'l':
                this.lineTo();
                break;
            case 'b':
                this.bezierTo();
                break;
            case 'f':
                this.fillPath();
                break;
            case 'd':
                this.drawPath();
                break;
            case 'C':
                this.clipPath();
                break;
            case 'x':
                this.transform();
                break;
            case 'z':
                this.save();
                break;
            case 'Z':
                this.restore();
                break;
        }
        // Move to beginning of next command.
        this._currentCommandIndex = this._commandList.indexOf(this._cmdTerminator, this._currentCommandIndex);
        if (this._currentCommandIndex > 0) {
            this._currentCommandIndex += 1;
        }
    }
    drawCommands(commands) {
        this._commandList = commands;
        this._currentCommandIndex = 0;
        //this._ctx.save(); 
        while (true) {
            if (this._currentCommandIndex < 0 || this._currentCommandIndex >= this._commandList.length)
                break;
            this.drawSingleCommand();
        }
        //this._ctx.restore();
    }
}
$(document).ready(function () {
    var canvas = document.getElementById("canvas");
    var ctx = canvas.getContext("2d");
    var drawer = new CanvasDrawer(ctx);
    var parser = new ParseOnlyCanvasDrawer();
    $("#button1").click(function () {
        return __awaiter(this, void 0, void 0, function* () {
            var startDownload = performance.now();
            var commandString = yield $.get("/Home/TestDrawMap");
            var endDownload = performance.now();
            var startRender = performance.now();
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            drawer.drawCommands(commandString);
            var endRender = performance.now();
            var startParse = performance.now();
            parser.drawCommands(commandString);
            var endParse = performance.now();
            $("#timingoutput").html("Done. Time to download: " + (endDownload - startDownload) + "ms. Time to render: " + (endRender - startRender) + "ms. " + " Time to parse: " + (endParse - startParse) + "ms.");
        });
    });
});
//# sourceMappingURL=canvasdraw.js.map