// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC20IMLayout.sol";
import "../ownable/OwnableLogic.sol";
import "../accessControlRef/AccessControlRefLogic.sol";
import "../reentrancy/ReentrancyLogic.sol";
import "../erc20/HERC20Logic.sol";
import "./HERC20IMInterface.sol";

import "../accessControl/AccessControlInterface.sol";

abstract contract HERC20IMLogic is HERC20IMLayout, OwnableLogic, AccessControlRefLogic, ReentrancyLogic, HERC20Logic, HERC20IMInterface {
    using SafeMath for uint256;

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoMint(){
        _sudoMint = true;
        _;
        _sudoMint = false;
    }

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoBurn(){
        _sudoBurn = true;
        _;
        _sudoBurn = false;
    }

    //for non-sudo mint, the "to" must be the msg.sender
    function _mintNormal(uint256 amount) internal onlyOnce {
        _mint(msg.sender, amount);
    }

    function _mintSudo(address to, uint256 amount) internal enableSudoMint onlyOnce {
        _mint(to, amount);
    }

    //for non-sudo burn, the "from" must be the msg.sender
    function _burnNormal(uint256 amount) internal onlyOnce {
        _burn(msg.sender, amount);
    }

    function _burnSudo(address from, uint256 amount) internal enableSudoBurn onlyOnce {
        _burn(from, amount);
    }

    //=====

    function setAccessControl(address accessControl_) override external onlyOwner {
        _setAccessControl(accessControl_);
        emit SetAccessControl(accessControl_);
    }

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) override external onlyOwner {
        _supportTransfer = supportTransfer_;
        _supportMint = supportMint_;
        _supportBurn = supportBurn_;
        emit SetSupport(supportTransfer_, supportMint_, supportBurn_);
    }

    function setBlockListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _blockListFrom[from[i]] = flag;
        }
    }

    function setBlockListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _blockListTo[to[i]] = flag;
        }
    }

    function setPrivilegeListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _privilegeListFrom[from[i]] = flag;
        }
    }

    function setPrivilegeListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _privilegeListTo[to[i]] = flag;
        }
    }
    //==================================

    function support() override view external returns (bool supportTransfer, bool supportMint, bool supportBurn){
        return (_supportTransfer, _supportMint, _supportBurn);
    }

    function transferTxs() override view external returns (uint256) {
        return _transferTxs;
    }

    function transferAmounts() override view external returns (uint256) {
        return _transferAmounts;
    }

    function interactAmounts() override view external returns (uint256){
        return _interactAmounts;
    }

    function isInteracted(address who) override view external returns (bool){
        return _interactAccount[who];
    }

    //privilege overrides block and supportTransfer
    function _beforeTokenTransfer(address from, address to, uint256 amount) virtual override(HERC20Logic) internal {

        if (from == address(0) && to != address(0)) {
            //mint mode
            //some how you call _mint :) , you must have done job of mint permission
            _beforeTokenTransferMint(to);
        }
        else if (from != address(0) && to == address(0)) {
            //burn mode
            //some how you call _burn :) , you must have done job of burn permission
            _beforeTokenTransferBurn(from);
        } else if (from != address(0) && to != address(0)) {
            //transfer
            _beforeTokenTransferTransfer(from, to);
        } else {
            revert("transfer, 'from' and 'to' should never be address(0) at same time");
        }

        //impossible to hit the 2^256 :)

    unchecked{_transferTxs = _transferTxs + 1;}


        //unreasonable to hit the 2^256 :)
        bool flag;
        (flag, _transferAmounts) = _transferAmounts.tryAdd(amount);
        if (flag) {
            _transferAmounts = type(uint256).max;
        }

        if (!_interactAccount[from]) {
            _interactAccount[from] = true;
        unchecked{_interactAmounts = _interactAmounts.add(1);}
        }

        HERC20Logic._beforeTokenTransfer(from, to, amount);

    }

    function _beforeTokenTransferMint(address to) view internal {
        AccessControlInterface ac = ac();

        //not support mint
        if (_sudoMint) {
            //pass
        } else {
            if (address(ac) != address(0) && ac.isPrivileged(to)) {
                //privilege pass
            } else if (_privilegeListTo[to]) {
                //privilege pass
            } else if (address(ac) != address(0) && ac.isBlocked(to)) {
                revert("ac.isBlocked-to");
            } else if (_blockListTo[to]) {
                revert("blockListTo");
            } else {
                if (_supportMint) {

                } else {
                    revert("un-support mint");
                }
            }
        }
    }

    function _beforeTokenTransferBurn(address from) view internal {
        AccessControlInterface ac = ac();

        if (_sudoBurn) {
            //pass
        } else {
            if (address(ac) != address(0) && ac.isPrivileged(from)) {
                //privilege overrides blockList
            } else if (_privilegeListFrom[from]) {
                //privilege overrides blockList
            } else if (address(ac) != address(0) && ac.isBlocked(from)) {
                revert("ac.isBlocked-from");
            } else if (_blockListFrom[from]) {
                revert("blockListFrom");
            } else {
                if (_supportBurn) {

                } else {
                    revert("un-support burn");
                }
            }
        }
    }

    function _beforeTokenTransferTransfer(address from, address to) view internal {
        AccessControlInterface ac = ac();

        if (address(ac) != address(0) &&
            (ac.isPrivileged(from) || ac.isPrivileged(to))
        ) {
            //privilege overrides blockList
        } else if (_privilegeListFrom[from] || _privilegeListTo[to]) {
            //privilege overrides blockList
        } else if (address(ac) != address(0) &&
            (ac.isBlocked(from) || ac.isBlocked(to))
        ) {
            revert("ac.isBlocked-from||ac.isBlocked-to");
        } else if (_blockListFrom[from] || _blockListTo[to]) {
            revert("blockListFrom||blockListTo");
        } else {
            if (_supportTransfer) {

            } else {
                revert("un-support transfer");
            }
        }
    }
}
