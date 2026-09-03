// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./NameServiceLayout.sol";
import "../../nameserver/accessControl/AccessControlLogic.sol";
import "./NameServiceInterface.sol";

contract NameServiceLogic is NameServiceLayout, AccessControlLogic, NameServiceInterface {

    using EnumerableSet for EnumerableSet.AddressSet;
    using EnumerableSet for EnumerableSet.Bytes32Set;

    function isMultiple(bytes32 keyName, address which) override view external returns (bool){
        return _multipleRegistry[keyName].contains(which);
    }

    function isMultipleSafe(bytes32 keyName, address which) override view external returns (bool){
        bool ret = _multipleRegistry[keyName].contains(which);
        require(ret, string(abi.encodePacked("isMultipleSafe, keyName: ", keyName, " address: ", which)));
        return ret;
    }

    function getSingle(bytes32 keyName) override view external returns (address){
        return _singleRegistry[keyName];
    }

    function getSingleSafe(bytes32 keyName) override view external returns (address){
        if (!_singleKeys.contains(keyName)) {
            revert(string(abi.encodePacked("getSingleSafe, keyName not set: ", keyName)));
        }
        address ret = _singleRegistry[keyName];
        require(ret != address(0), string(abi.encodePacked("getSingleSafe, keyValue not set: ", keyName)));
        return ret;
    }

    //==========

    function setMultiple(bytes32 keyName, address which, bool enable) override external onlyOwner {
        if (enable) {
            if (_multipleRegistry[keyName].length() == 0) {
                _multipleKeys.add(keyName);
            }
            _multipleRegistry[keyName].add(which);
        } else {
            _multipleRegistry[keyName].remove(which);
            if (_multipleRegistry[keyName].length() == 0) {
                _multipleKeys.remove(keyName);
            }
        }
    }

    function setMultipleEntries(bytes32[] memory keyName, address[] memory which, bool[] memory enable) override external onlyOwner {
        for (uint256 i = 0; i < keyName.length; i ++) {
            if (enable[i]) {
                if (_multipleRegistry[keyName[i]].length() == 0) {
                    _multipleKeys.add(keyName[i]);
                }
                _multipleRegistry[keyName[i]].add(which[i]);
            } else {
                _multipleRegistry[keyName[i]].remove(which[i]);
                if (_multipleRegistry[keyName[i]].length() == 0) {
                    _multipleKeys.remove(keyName[i]);
                }
            }
        }
    }

    //the 'which' maybe zero
    function setSingle(bytes32 keyName, address which, bool enable) override external onlyOwner {
        if (enable) {
            if (!_singleKeys.contains(keyName)) {
                _singleKeys.add(keyName);
            }
            _singleRegistry[keyName] = which;
        } else {
            require(which == address(0), "setSingle must be 0 while disable the key");
            if (_singleKeys.contains(keyName)) {
                _singleKeys.remove(keyName);
            }
            _singleRegistry[keyName] = address(0);

        }
    }

    function setSingleEntries(bytes32[] memory keyName, address[] memory which, bool[] memory enable) override external onlyOwner {
        for (uint256 i = 0; i < keyName.length; i ++) {
            if (enable[i]) {
                if (!_singleKeys.contains(keyName[i])) {
                    _singleKeys.add(keyName[i]);
                }
                _singleRegistry[keyName[i]] = which[i];
            } else {
                require(which[i] == address(0), "setSingle-s must be 0 while disable the key");
                if (_singleKeys.contains(keyName[i])) {
                    _singleKeys.remove(keyName[i]);
                }
                _singleRegistry[keyName[i]] = address(0);
            }
        }
    }

    function listSingleEntries() override view external returns (NameServiceType.SingleEntryRet[] memory){

        NameServiceType.SingleEntryRet[] memory ret = new NameServiceType.SingleEntryRet[](_singleKeys.length());

        for (uint256 i = 0; i < _singleKeys.length(); i++) {
            ret[i].name = _singleKeys.at(i);
            ret[i].addr = _singleRegistry[ret[i].name];
        }

        return ret;
    }

    function listMultipleEntries() override view external returns (NameServiceType.MultipleEntryRet[] memory){

        NameServiceType.MultipleEntryRet[] memory ret = new NameServiceType.MultipleEntryRet[](_multipleKeys.length());

        for (uint256 i = 0; i < _multipleKeys.length(); i++) {
            bytes32 name = _multipleKeys.at(i);
            ret[i].name = name;

            ret[i].addr = new address[](_multipleRegistry[name].length());

            for (uint256 j = 0; j < _multipleRegistry[name].length(); j++) {
                ret[i].addr[j] = _multipleRegistry[name].at(j);
            }
        }

        return ret;
    }
}
