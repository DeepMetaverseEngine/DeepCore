// SPDX-License-Identifier: MIT

pragma solidity ^0.8.0;

import "./DeputyRefLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "./DeputyRefInterface.sol";

import "../deputyCenter/DeputyCenterInterface.sol";
import "../../nameserver/nameService/NameServiceInterface.sol";
import "../../nameserver/nameService/NameServiceType.sol";

import "../helperLibrary/StringsLibrary.sol";
import "@openzeppelin/contracts/utils/Strings.sol";

contract DeputyRefLogic is DeputyRefLayout, NameServiceRefLogic, DeputyRefInterface {

    //remember to call this modifier at first to check msg.value
    //this function IGNORES designatedSender, the business logic could add this in param
    modifier onlySignerIsServer(){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        require(signer != address(0), string(abi.encodePacked("deputy: parse deputy fails: ", reason)));
        require(isServer(signer), "deputy: signer is not authed server");
        uint256 msgValue;
        assembly{
            msgValue := callvalue()
        }
        require(value <= msgValue, "deputy: value mismatch");
        _;
    }

    modifier onlySignerIsSpecified(address _signer_){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        require(signer != address(0), string(abi.encodePacked("deputy: parse deputy fails: ", reason)));
        require(signer == _signer_, "deputy: _signer_ is not desired");
        uint256 msgValue;
        assembly{
            msgValue := callvalue()
        }
        require(value <= msgValue, "deputy: value mismatch");
        _;
    }

    modifier onlySignerIsServerAndDesignatedSenderIsSpecified(address _designatedSender_){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        require(signer != address(0), string(abi.encodePacked("deputy: parse deputy fails: ", reason)));
        require(isServer(signer), "deputy: signer is not authed server");
        require(designatedSender == _designatedSender_, "deputy: _designatedSender_ mismatch");

        uint256 msgValue;
        assembly{
            msgValue := callvalue()
        }
        require(value <= msgValue, "deputy: value mismatch");
        _;
    }

    modifier onlySelfSignerIsServer(){

        if (msg.sender == address(this)) {
            //fine
        } else {

            (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
            require(signer != address(0), string(abi.encodePacked("deputy: parse deputy fails: ", reason)));
            require(isServer(signer), "deputy: signer is not authed server");

            uint256 msgValue;
            assembly{
                msgValue := callvalue()
            }
            require(value <= msgValue, "deputy: value mismatch");
        }
        _;
    }

    modifier onlySelfSignerIsServerOrMsgDotSenderIsSpecified(address _specified_msg_sender_){

        if (msg.sender == address(this)) {
            //fine
        } else if (msg.sender == _specified_msg_sender_) {
            //fine
        } else {

            (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
            require(signer != address(0), string(abi.encodePacked("deputy: parse deputy fails: ", reason)));
            require(isServer(signer), "deputy: signer is not authed server");

            uint256 msgValue;
            assembly{
                msgValue := callvalue()
            }
            require(value <= msgValue, "deputy: value mismatch");
        }
        _;
    }

    modifier onlySelfSignerIsServerAndDesignatedSenderIsSpecified(address _designatedSender_){

        if (msg.sender == address(this)) {
            //fine
        } else {

            (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
            require(signer != address(0), string(abi.encodePacked("deputy: parse deputy fails: ", reason)));
            require(isServer(signer), "deputy: signer is not authed server");
            require(designatedSender == _designatedSender_, "deputy: _designatedSender_ mismatch");

            uint256 msgValue;
            assembly{
                msgValue := callvalue()
            }
            require(value <= msgValue, "deputy: value mismatch");
        }
        _;
    }

    modifier onlySignerIsSpecifiedAndDesignatedSenderIsSpecified(address _signer_, address _designatedSender_){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        require(signer != address(0), string(abi.encodePacked("deputy: parse deputy fails: ", reason)));
        require(signer == _signer_, "deputy: _signer_ is not desired");
        require(designatedSender == _designatedSender_, "deputy: desiredDesignatedSender mismatch");

        uint256 msgValue;
        assembly{
            msgValue := callvalue()
        }
        require(value <= msgValue, "deputy: value mismatch");
        _;
    }

    modifier onlyBatchTx(){
        require(msg.sender == deputyCenter(), "deputy: tx not called from deputyCenter.dispatchTransactions");
        _;
    }

    modifier onlyBatchTxCallerIsSpecified(address _caller_){
        require(msg.sender == deputyCenter(), "deputy: tx not called from deputyCenter.dispatchTransactions");
        (bool called, address caller) = DeputyCenterInterface(deputyCenter()).calledAndCaller();
        require(called, "deputy: parse deputy fails, called from deputyCenter, but 'called' is false?");
        require(caller == _caller_, "deputy: parse deputy fails, but caller != _caller_");
        _;
    }

    modifier onlyMsgDotSenderOrDesignatedSenderIsSpecified(address _signerIfDeputy_, address _who_){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        if (signer == address(0)) {
            //check deputy fails
            require(msg.sender == _who_, string(abi.encodePacked("deputy: parse deputy fails, but msg.sender != _who_, reason: ", reason)));
        } else {
            //check deputy success
            require(signer == _signerIfDeputy_, "deputy: _signerIfDeputy_ is not desired");
            require(designatedSender == _who_, "deputy: parse deputy success, but designatedSender != _who_");
            uint256 msgValue;
            assembly{
                msgValue := callvalue()
            }
            require(value <= msgValue, "value mismatch");
        }
        _;
    }

    modifier onlyMsgDotSenderOrDesignatedSenderIsSpecifiedSignerIsServer(address _who_){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        if (signer == address(0)) {
            //check deputy fails
            require(msg.sender == _who_, string(abi.encodePacked("deputy: parse deputy fails, but msg.sender != _who_, reason: ", reason)));
        } else {
            //check deputy success
            require(isServer(signer), "deputy: signer is not authed server");
            require(designatedSender == _who_, "deputy: parse deputy success, but designatedSender != _who_");
            uint256 msgValue;
            assembly{
                msgValue := callvalue()
            }
            require(value <= msgValue, "value mismatch");
        }
        _;
    }

    modifier onlyMsgDotSenderOrDesignatedSenderOrBatchTxCallerIsSpecified(address _signerIfDeputy_, address _who_){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        if (signer == address(0)) {
            //check deputy fails
            if (msg.sender == deputyCenter()) {
                //it is a batch tx call
                (bool called, address caller) = DeputyCenterInterface(deputyCenter()).calledAndCaller();
                require(called, "deputy: parse deputy fails, called from deputyCenter, but 'called' is false?");
                require(caller == _who_, "deputy: parse deputy fails, but caller != _who_");
            } else {
                require(msg.sender == _who_, "deputy: parse deputy fails, but msg.sender != _who_");
            }
        } else {
            //check deputy success
            require(signer == _signerIfDeputy_, "deputy: _signerIfDeputy_ is not desired");
            require(designatedSender == _who_, "deputy: parse deputy success, but designatedSender != _who_");

            uint256 msgValue;
            assembly{
                msgValue := callvalue()
            }
            require(value <= msgValue, "value mismatch");
        }
        _;
    }

    modifier onlyMsgDotSenderOrDesignatedSenderOrBatchTxCallerIsSpecifiedSignerIsServer(address _who_){

        (address designatedSender, address signer, uint256 value, string memory reason) = _checkDeputy();
        if (signer == address(0)) {
            //check deputy fails
            if (msg.sender == deputyCenter()) {
                //it is a batch tx call
                (bool called, address caller) = DeputyCenterInterface(deputyCenter()).calledAndCaller();
                require(called, "deputy: parse deputy fails, called from deputyCenter, but 'called' is false?");
                require(caller == _who_, "deputy: parse deputy fails, but caller != _who_");
            } else {
                require(msg.sender == _who_, "deputy: parse deputy fails, but msg.sender != _who_");
            }
        } else {
            //check deputy success
            require(isServer(signer), "deputy: signer is not authed server");
            require(designatedSender == _who_, "deputy: parse deputy success, but designatedSender != _who_");

            uint256 msgValue;
            assembly{
                msgValue := callvalue()
            }
            require(value <= msgValue, "value mismatch");
        }
        _;
    }

    function _checkDeputy() internal returns (address, address, uint256, string memory){
        bytes memory callData = msg.data;

        (bool success, bytes memory returnData) = deputyCenter().call(callData);

        require(success, string(abi.encodePacked("deputy, error: ", sysPrintBytesToHex(returnData))));

        (address designatedSender, address signer, uint256 value,string memory reason) = abi.decode(returnData, (address, address, uint256, string));

        return (designatedSender, signer, value, reason);
    }


    function sysPrintBytesToHex(bytes memory input) internal pure returns (string memory){
        bytes memory ret = new bytes(input.length * 2);
        bytes memory alphabet = "0123456789abcdef";
        for (uint256 i = 0; i < input.length; i++) {
            bytes32 t = bytes32(input[i]);
            bytes32 tt = t >> 31 * 8;
            uint256 b = uint256(tt);
            uint256 high = b / 0x10;
            uint256 low = b % 0x10;
            bytes1 highAscii = alphabet[high];
            bytes1 lowAscii = alphabet[low];
            ret[2 * i] = highAscii;
            ret[2 * i + 1] = lowAscii;
        }
        return string(ret);
    }
}
