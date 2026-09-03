// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AccessControlLayout.sol";
import "../../nameserver/ownable/OwnableLogic.sol";
import "./AccessControlInterface.sol";

contract AccessControlLogic is AccessControlLayout, OwnableLogic, AccessControlInterface {


    function isBlocked(address which) override view external returns (bool){
        return _blockList[which];
    }

    //privilege overrides block
    function isPrivileged(address which) override view external returns (bool){
        return _privilegeList[which];
    }

    //==========

    function setBlockList(address[] memory list, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < list.length; i++) {
            _blockList[list[i]] = flag;
        }
    }

    //privilege overrides block
    function setPrivilegedList(address[] memory list, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < list.length; i++) {
            _privilegeList[list[i]] = flag;
        }
    }
}
