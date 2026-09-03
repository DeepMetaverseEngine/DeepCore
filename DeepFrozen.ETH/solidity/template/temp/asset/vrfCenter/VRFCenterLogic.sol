// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./VRFCenterLayout.sol";
import "../ownable/OwnableLogic.sol";
import "./VRFCenterInterface.sol";

import "../helperLibrary/vrfLibrary/VRF.sol";

contract VRFCenterLogic is VRFCenterLayout, OwnableLogic, VRFCenterInterface {

    using SafeMath for uint256;

    function requestVrf(address who, bytes32 reason) override external returns (bytes32 /*requestId*/) {
        uint256 nonce = _vrfTriple[msg.sender][who][reason];

        //fix alpha as bytes32 as requestId
        bytes32 alpha = keccak256(abi.encode(msg.sender, who, reason, nonce, _vrfGlobalNonce));
        require(!_vrfAlphaString[alpha], "vrf center: request id duplicated, or has been requested");
        _vrfAlphaString[alpha] = true;
        _vrfCallBack[alpha] = msg.sender;

        _vrfTriple[msg.sender][who][reason] = nonce.add(1);
        _vrfGlobalNonce = _vrfGlobalNonce.add(1);

        emit RequestVRF(alpha);

        return alpha;
    }

    //do remember to lock 'something' first
    function verifyVrf(
        bytes32 requestId, //alpha
        uint256[2] memory publicKey, //Y:x, Y:y
        uint256[4] memory proof, //D, a.k.a.decoded Pi, Gamma:x, Gamma:y, c, s
        uint256[2] memory uPoint, //U:x, U:y
        uint256[4] memory vComponents//s*H:x, s*H:y, c*Gamma:x, c*Gamma:y
    ) override public returns (bytes32 randomness){

        randomness = _verifyVrf(requestId, publicKey, proof, uPoint, vComponents);
        address callBack = _vrfCallBack[requestId];
        require(callBack != address(0), "vrf center: call back address is zero");
        require(callBack == msg.sender, "vrf center: verify vrf callBack should be msg.sender as proactive invoke");

        return randomness;
    }

    function feedVrf(
        bytes32 requestId, //alpha
        uint256[2] memory publicKey, //Y:x, Y:y
        uint256[4] memory proof, //D, a.k.a.decoded Pi, Gamma:x, Gamma:y, c, s
        uint256[2] memory uPoint, //U:x, U:y
        uint256[4] memory vComponents//s*H:x, s*H:y, c*Gamma:x, c*Gamma:y
    ) override external {

        bytes32 randomness = _verifyVrf(requestId, publicKey, proof, uPoint, vComponents);
        address callBack = _vrfCallBack[requestId];
        require(callBack != address(0), "vrf center: call back address is zero");
        require(callBack != msg.sender, "vrf center: feed vrf callBack should not be msg.sender");

        (bool success, bytes memory returnData) = address(callBack).call(abi.encodeWithSignature("rawFulfillRandomness(bytes32,bytes32)", requestId, randomness));
        success;
        returnData;
    }

    function _verifyVrf(
        bytes32 requestId, //alpha
        uint256[2] memory publicKey, //Y:x, Y:y
        uint256[4] memory proof, //D, a.k.a.decoded Pi, Gamma:x, Gamma:y, c, s
        uint256[2] memory uPoint, //U:x, U:y
        uint256[4] memory vComponents//s*H:x, s*H:y, c*Gamma:x, c*Gamma:y
    ) internal returns (bytes32 randomness){

        require(_vrfAlphaString[requestId], "vrf center: request id has not been requested");

        if (_vrfBetaString[requestId] != bytes32(0)) {
            return _vrfBetaString[requestId];
        }

        require(isVRFProvider(publicKey), "vrf center: provider is invalid");

        //verify the proof
        require(
            VRF.fastVerify(
                publicKey,
                proof,
                abi.encodePacked(requestId),
                uPoint,
                vComponents
            ),
            "vrf center: vrf proof verify fails"
        );

        randomness = VRF.gammaToHash(proof[0], proof[1]);
        _vrfBetaString[requestId] = randomness;

        emit VerifyVRF(requestId, randomness);

        return randomness;
    }

    function vrfAlphaString(bytes32 requestId) override view external returns (bool){
        return _vrfAlphaString[requestId];
    }

    function vrfBetaString(bytes32 requestId) override view external returns (bytes32){
        return _vrfBetaString[requestId];
    }

    function setVRFProvider(uint256[2] memory publicKey, bool flag) override external onlyOwner {
        _vrfProvider[publicKey[0]][publicKey[1]] = flag;
    }

    function isVRFProvider(uint256[2] memory publicKey) override view public returns (bool){
        return _vrfProvider[publicKey[0]][publicKey[1]];
    }
}
