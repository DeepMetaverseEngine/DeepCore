// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./NameServiceRefLayout.sol";
import "../../nameserver/accessControlRef/AccessControlRefLogic.sol";
import "./NameServiceRefInterface.sol";

import "../../nameserver/nameService/NameServiceInterface.sol";
import "../../nameserver/nameService/NameServiceType.sol";

contract NameServiceRefLogic is NameServiceRefLayout, AccessControlRefLogic, NameServiceRefInterface {

    function ns() view internal returns (NameServiceInterface){
        require(_accessControl != address(0), "ns(): address 0");
        return NameServiceInterface(_accessControl);
    }

    function nsUnsafe() view internal returns (NameServiceInterface){
        return NameServiceInterface(_accessControl);
    }

    function manager() view internal returns (address){
        return ns().getSingleSafe(NameServiceType.S_Manager);
    }

    function miner() view internal returns (address){
        return ns().getSingleSafe(NameServiceType.S_Miner);
    }

    function deputyCenter() view internal returns (address){
        return ns().getSingleSafe(NameServiceType.S_DeputyCenter);
    }

    function vrfCenter() view internal returns (address){
        return ns().getSingleSafe(NameServiceType.S_VRFCenter);
    }

    function vrfSigner() view internal returns (address){
        return ns().getSingleSafe(NameServiceType.S_VRFSigner);
    }

    function assetVault() view internal returns (address){
        return ns().getSingleSafe(NameServiceType.S_AssetVault);
    }

    function isServer(address input) view internal returns (bool){
        return ns().isMultiple(NameServiceType.M_Server, input);
    }

    function _setNameService(address ns_) internal {
        _setAccessControl(ns_);
    }
}
